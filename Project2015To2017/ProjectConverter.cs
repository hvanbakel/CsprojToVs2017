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
	public class ProjectConverter
	{
		private static IReadOnlyList<ITransformation> TransformationsToApply(ConversionOptions conversionOptions, Project project)
		{
			if (project.IsModernProject)
			{
				return new ITransformation[]
				{
					new FileTransformation(),
				};
			}

			return new ITransformation[]
			{
				new TargetFrameworkTransformation(
					conversionOptions.TargetFrameworks,
					conversionOptions.AppendTargetFrameworkToOutputPath),
				new PackageReferenceTransformation(),
				new AssemblyReferenceTransformation(),
				new RemovePackageAssemblyReferencesTransformation(),
				new RemovePackageImportsTransformation(),
				new FileTransformation(),
				new NugetPackageTransformation(),
				new AssemblyAttributeTransformation(conversionOptions.KeepAssemblyInfo),
				new XamlPagesTransformation()
			};
		}

		public static IEnumerable<Project> Convert(
			string target,
			IProgress<string> progress)
		{
			return Convert(target, new List<ITransformation>(), new List<ITransformation>(), progress);
		}

		public static IEnumerable<Project> Convert(
			string target,
			ConversionOptions conversionOptions,
			IProgress<string> progress)
		{
			return Convert(target, conversionOptions, new List<ITransformation>(), new List<ITransformation>(), progress);
		}

		public static IEnumerable<Project> Convert(
			string target,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress
		)
		{
			return Convert(target, new ConversionOptions(), preTransforms, postTransforms, progress);
		}

		public static IEnumerable<Project> Convert(
			string target,
			ConversionOptions conversionOptions,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress
		)
		{
			var extension = Path.GetExtension(target) ?? throw new ArgumentNullException(nameof(target));
			if (extension.Length > 0)
			{
				extension = extension.ToLowerInvariant();
				switch (extension)
				{
					case ".sln":
						foreach (var project in ConvertSolution(target, conversionOptions, preTransforms, postTransforms, progress))
						{
							yield return project;
						}
						break;

					case ".csproj":
						var file = new FileInfo(target);
						yield return ProcessFile(file, null, conversionOptions, preTransforms, postTransforms, progress);
						break;

					default:
						progress.Report("Please specify a project or solution file.");
						break;
				}

				yield break;
			}

			// Process the only solution in given directory
			var solutionFiles = Directory.EnumerateFiles(target, "*.sln", SearchOption.TopDirectoryOnly).ToArray();
			if (solutionFiles.Length == 1)
			{
				foreach (var project in ConvertSolution(solutionFiles[0], conversionOptions, preTransforms, postTransforms, progress))
				{
					yield return project;
				}

				yield break;
			}

			// Process all csprojs found in given directory
			var projectFiles = Directory.EnumerateFiles(target, "*.csproj", SearchOption.AllDirectories).ToArray();
			if (projectFiles.Length == 0)
			{
				progress.Report("Please specify a project file.");
				yield break;
			}

			progress.Report($"Multiple project files found under directory {target}:");
			progress.Report(string.Join(Environment.NewLine, projectFiles));
			foreach (var projectFile in projectFiles)
			{
				// todo: rewrite both directory enumerations to use FileInfo instead of raw strings
				yield return ProcessFile(new FileInfo(projectFile), null, conversionOptions, preTransforms, postTransforms, progress);
			}
		}

		private static IEnumerable<Project> ConvertSolution(string target, ConversionOptions conversionOptions,
			IReadOnlyList<ITransformation> preTransforms, IReadOnlyList<ITransformation> postTransforms, IProgress<string> progress)
		{
			progress.Report("Solution parsing started.");
			var solution = SolutionReader.Instance.Read(target, progress);

			if (solution.ProjectPaths == null)
			{
				yield break;
			}

			foreach (var projectPath in solution.ProjectPaths)
			{
				progress.Report("Project found: " + projectPath.Include);
				if (!projectPath.ProjectFile.Exists)
				{
					progress.Report("Project file not found at: " + projectPath.ProjectFile.FullName);
				}
				else
				{
					yield return ProcessFile(projectPath.ProjectFile, solution, conversionOptions, preTransforms, postTransforms,
						progress);
				}
			}
		}

		[Obsolete]
		private static Project ProcessFile(
			string filePath,
			Solution solution,
			ConversionOptions conversionOptions,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress
		) => ProcessFile(new FileInfo(filePath), solution, conversionOptions, preTransforms, postTransforms, progress);

		private static Project ProcessFile(
				FileInfo file,
				Solution solution,
				ConversionOptions conversionOptions,
				IReadOnlyList<ITransformation> preTransforms,
				IReadOnlyList<ITransformation> postTransforms,
				IProgress<string> progress
			)
		{
			if (!Validate(file, progress))
			{
				return null;
			}

			var project = new ProjectReader(file, progress).Read();
			if (project == null)
			{
				return null;
			}

			project.Solution = solution;

			foreach (var transform in preTransforms)
			{
				transform.Transform(project, progress);
			}

			foreach (var transform in TransformationsToApply(conversionOptions, project))
			{
				transform.Transform(project, progress);
			}

			foreach (var transform in postTransforms)
			{
				transform.Transform(project, progress);
			}

			return project;
		}

		internal static bool Validate(FileInfo file, IProgress<string> progress)
		{
			if (file.Exists)
			{
				return true;
			}

			progress.Report($"File {file.FullName} could not be found.");
			return false;

		}
	}
}
