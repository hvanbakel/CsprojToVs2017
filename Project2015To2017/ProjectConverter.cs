using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
	public sealed class ProjectConverter
	{
		public static readonly IReadOnlyDictionary<string, string> ProjectFileMappings = new Dictionary<string, string>
		{
			{ ".csproj", "cs" },
			{ ".vbproj", "vb" },
			{ ".fsproj", "fs" }
		};

		private readonly ILogger logger;
		private readonly ConversionOptions conversionOptions;
		private readonly ProjectReader projectReader;
		private readonly ITransformationSet transformationSet;

		public ProjectConverter(
			ILogger logger,
			ITransformationSet transformationSet,
			ConversionOptions conversionOptions = null)
		{
			this.logger = logger;
			this.conversionOptions = conversionOptions ?? new ConversionOptions();
			this.transformationSet = transformationSet ?? BasicReadTransformationSet.Instance;
			this.projectReader = new ProjectReader(logger, this.conversionOptions);
		}

		public IEnumerable<Project> Convert(string target)
		{
			var extension = Path.GetExtension(target) ?? throw new ArgumentNullException(nameof(target));
			if (extension.Length > 0)
			{
				var file = new FileInfo(target);
				switch (extension)
				{
					case ".sln":
						{
						var solution = SolutionReader.Instance.Read(file, this.logger);
						foreach (var project in ProcessSolutionFile(solution))
						{
							yield return project;
						}
						break;
					}
					case string s when ProjectFileMappings.ContainsKey(extension):
					{
						yield return this.ProcessProjectFile(file, null);
						break;
					}
					default:
					{
						this.logger.LogCritical("Please specify a project or solution file.");
						break;
				}
				}

				yield break;
			}

			// Process the only solution in given directory
			var solutionFiles = Directory.EnumerateFiles(target, "*.sln", SearchOption.TopDirectoryOnly).ToArray();
			if (solutionFiles.Length == 1)
			{
				var solution = SolutionReader.Instance.Read(solutionFiles[0], this.logger);
				foreach (var project in this.ProcessSolutionFile(solution))
				{
					yield return project;
				}

				yield break;
			}

			var projectsProcessed = 0;
			// Process all csprojs found in given directory
			foreach (var mapping in ProjectFileMappings)
			{
				var projectFiles = Directory.EnumerateFiles(target, "*" + mapping.Key, SearchOption.AllDirectories).ToArray();
				if (projectFiles.Length == 0)
				{
					continue;
				}

				if (projectFiles.Length > 1)
				{
					this.logger.LogInformation($"Multiple project files found under directory {target}:");
				}

				this.logger.LogInformation(string.Join(Environment.NewLine, projectFiles));

				foreach (var projectFile in projectFiles)
				{
					yield return this.ProcessProjectFile(new FileInfo(projectFile), null);
					projectsProcessed++;
				}
			}

			if (projectsProcessed == 0)
			{
				this.logger.LogCritical("Please specify a project file.");
			}
		}

		public IEnumerable<Project> ProcessSolutionFile(Solution solution)
		{
			this.logger.LogTrace("Solution parsing started.");
			if (solution.ProjectPaths == null)
			{
				this.logger.LogTrace($"'{nameof(solution.ProjectPaths)}' is null");
				yield break;
			}

			foreach (var projectReference in solution.ProjectPaths)
			{
				this.logger.LogInformation("Project found: " + projectReference.Include);
				if (!projectReference.ProjectFile.Exists)
				{
					this.logger.LogError("Project file not found at: " + projectReference.ProjectFile.FullName);
					continue;
				}

				yield return this.ProcessProjectFile(projectReference.ProjectFile, solution, projectReference);
			}
		}

		public Project ProcessProjectFile(FileInfo file, Solution solution, ProjectReference reference = null)
		{
			if (!Validate(file, this.logger))
			{
				return null;
			}

			var project = this.projectReader.Read(file);
			if (project == null)
			{
				return null;
			}

			project.CodeFileExtension = ProjectFileMappings[file.Extension];
			if (reference?.ProjectName != null)
			{
				project.ProjectName = reference.ProjectName;
			}

			project.Solution = solution;

			foreach (var transform in this.conversionOptions.PreDefaultTransforms)
			{
				transform.Transform(project);
			}

			foreach (var transform in TransformationsToApply())
			{
				if (project.IsModernProject
				    && transform is ILegacyOnlyProjectTransformation
				    && !this.conversionOptions.ForceDefaultTransforms.Contains(transform.GetType().Name))
				{
					continue;
				}

				if (!project.IsModernProject
				    && transform is IModernOnlyProjectTransformation
				    && !this.conversionOptions.ForceDefaultTransforms.Contains(transform.GetType().Name))
				{
					continue;
				}

				transform.Transform(project);
			}

			foreach (var transform in this.conversionOptions.PostDefaultTransforms)
			{
				transform.Transform(project);
			}

			return project;
		}

		internal static bool Validate(FileInfo file, ILogger logger)
		{
			if (file.Exists)
			{
				return true;
			}

			logger.LogError($"File {file.FullName} could not be found.");
			return false;
		}

		private IReadOnlyCollection<ITransformation> TransformationsToApply()
		{
			var all = this.transformationSet.Transformations(this.logger, this.conversionOptions);
			var (normal, others) = all.Split(FilterTargetNormalTransformations);
			var (early, late) = others.Split(x =>
				((ITransformationWithTargetMoment) x).ExecutionMoment == TargetTransformationExecutionMoment.Early);
			var res = new List<ITransformation>(all.Count);
			TopologicalSort(early, res, this.logger);
			TopologicalSort(normal, res, this.logger);
			TopologicalSort(late, res, this.logger);
			return res;
		}

		private static void TopologicalSort(
			IReadOnlyList<ITransformation> source,
			ICollection<ITransformation> target,
			ILogger logger)
		{
			var count = source.Count;
			if (count == 0)
			{
				return;
			}

			// When Span<T> becomes available - replace with
			// var used = count <= 256 ? stackalloc byte[count] : new byte[count];
			var used = new byte[count];
			var res = new LinkedList<ITransformation>();
			var mappings = new Dictionary<string, int>();
			for (var i = 0; i < count; i++)
			{
				var transformation = source[i];
				if (transformation == null)
				{
					throw new ArgumentNullException(nameof(transformation),
						"Transformation set must not contain null items");
				}

				mappings.Add(transformation.GetType().Name, i);
			}

			for (var i = 0; i < count; i++)
			{
				if (used[i] != 0)
				{
					continue;
				}

				TopologicalSortInternal(source, i, used, mappings, res, logger);
			}

			// topological order on reverse graph is reverse topological order on the original
			var item = res.Last;
			while (item != null)
			{
				target.Add(item.Value);
				item = item.Previous;
			}
		}

		private static void TopologicalSortInternal(
			IReadOnlyList<ITransformation> source,
			int vertex,
			byte[] used,
			IDictionary<string, int> mappings,
			LinkedList<ITransformation> res,
			ILogger logger)
		{
			if (used[vertex] == 1)
			{
				throw new InvalidOperationException(
					"Transformation set contains dependency cycle, DAG is required to build transformation tree");
			}

			if (used[vertex] != 0)
			{
				return;
			}

			used[vertex] = 1;
			var item = source[vertex];
			var name = item.GetType().Name;
			if (item is ITransformationWithDependencies itemWithDependencies)
			{
				foreach (var dependencyName in itemWithDependencies.DependOn)
				{
					if (!mappings.TryGetValue(dependencyName, out var mapping))
					{
						logger.LogWarning($"Unable to find {dependencyName} as dependency for {name}");
						continue;
					}

					TopologicalSortInternal(source, mapping, used, mappings, res, logger);
				}
			}

			used[vertex] = 2;
			res.AddFirst(item);
		}

		private static bool FilterTargetNormalTransformations(ITransformation x)
		{
			if (x is ITransformationWithTargetMoment m)
			{
				return m.ExecutionMoment == TargetTransformationExecutionMoment.Normal;
			}

			return true;
		}
	}
}
