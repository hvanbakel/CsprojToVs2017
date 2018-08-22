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
		private readonly ILogger logger;
		private readonly ConversionOptions conversionOptions;
		private readonly ProjectReader projectReader;

		private static IReadOnlyCollection<ITransformation>
			TransformationsToApply(ConversionOptions conversionOptions, bool modernProject)
		{
			var targetFrameworkTransformation = new TargetFrameworkTransformation(
				conversionOptions.TargetFrameworks,
				conversionOptions.AppendTargetFrameworkToOutputPath);

			if (modernProject)
			{
				return new ITransformation[]
				{
					targetFrameworkTransformation
				};
			}

			return new ITransformation[]
			{
				targetFrameworkTransformation,
				new PropertySimplificationTransformation(),
				new PropertyDeduplicationTransformation(),
				new TestProjectPackageReferenceTransformation(),
				new AssemblyReferenceTransformation(),
				new RemovePackageAssemblyReferencesTransformation(),
				new DefaultAssemblyReferenceRemovalTransformation(),
				new RemovePackageImportsTransformation(),
				new FileTransformation(),
				new NugetPackageTransformation(),
				new AssemblyAttributeTransformation(conversionOptions.KeepAssemblyInfo),
				new XamlPagesTransformation(),
				new PrimaryUnconditionalPropertyTransformation(),
				new EmptyGroupRemoveTransformation(),
			};
		}

		public ProjectConverter(ILogger logger, ConversionOptions conversionOptions = null)
		{
			this.logger = logger;
			this.conversionOptions = conversionOptions ?? new ConversionOptions();
			this.projectReader = new ProjectReader(logger, this.conversionOptions);
		}

		public IEnumerable<Project> Convert(string target)
		{
			var extension = Path.GetExtension(target) ?? throw new ArgumentNullException(nameof(target));
			if (extension.Length > 0)
			{
				extension = extension.ToLowerInvariant();
				switch (extension)
				{
					case ".sln":
						foreach (var project in ConvertSolution(target))
						{
							yield return project;
						}

						break;

					case ".csproj":
						var file = new FileInfo(target);
						yield return this.ProcessFile(file, null);
						break;

					default:
						this.logger.LogCritical("Please specify a project or solution file.");
						break;
				}

				yield break;
			}

			// Process the only solution in given directory
			var solutionFiles = Directory.EnumerateFiles(target, "*.sln", SearchOption.TopDirectoryOnly).ToArray();
			if (solutionFiles.Length == 1)
			{
				foreach (var project in this.ConvertSolution(solutionFiles[0]))
				{
					yield return project;
				}

				yield break;
			}

			// Process all csprojs found in given directory
			var projectFiles = Directory.EnumerateFiles(target, "*.csproj", SearchOption.AllDirectories).ToArray();
			if (projectFiles.Length == 0)
			{
				this.logger.LogCritical("Please specify a project file.");
				yield break;
			}

			if (projectFiles.Length > 1)
			{
				this.logger.LogInformation($"Multiple project files found under directory {target}:");
			}

			this.logger.LogInformation(string.Join(Environment.NewLine, projectFiles));

			foreach (var projectFile in projectFiles)
			{
				// todo: rewrite both directory enumerations to use FileInfo instead of raw strings
				yield return this.ProcessFile(new FileInfo(projectFile), null);
			}
		}

		private IEnumerable<Project> ConvertSolution(string target)
		{
			this.logger.LogDebug("Solution parsing started.");
			var solution = SolutionReader.Instance.Read(target, this.logger);

			if (solution.ProjectPaths == null)
			{
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

				yield return this.ProcessFile(projectReference.ProjectFile, solution, projectReference);
			}
		}

		private Project ProcessFile(FileInfo file, Solution solution, ProjectReference reference = null)
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

			if (reference?.ProjectName != null)
			{
				project.ProjectName = reference.ProjectName;
			}

			project.Solution = solution;

			foreach (var transform in this.conversionOptions.PreDefaultTransforms)
			{
				transform.Transform(project, this.logger);
			}

			foreach (var transform in TransformationsToApply(this.conversionOptions, project.IsModernProject))
			{
				transform.Transform(project, this.logger);
			}

			foreach (var transform in this.conversionOptions.PostDefaultTransforms)
			{
				transform.Transform(project, this.logger);
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
	}
}