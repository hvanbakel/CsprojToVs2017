using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Project2015To2017.Definition;
using Project2015To2017.Reading;
using Project2015To2017.Transforms;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
	public class ProjectConverter
	{
		private static IReadOnlyList<ITransformation> TransformationsToApply(ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				new PackageReferenceTransformation(),
				new AssemblyReferenceTransformation(),
				new RemovePackageAssemblyReferencesTransformation(),
				new RemovePackageImportsTransformation(),
				new FileTransformation(),
				new NugetPackageTransformation(),
				new AssemblyAttributeTransformation
				{
					KeepAssemblyInfoFile = conversionOptions.KeepAssemblyInfo
				}
			};
		}

		public static IEnumerable<Definition.Project> Convert(
			string target,
			IProgress<string> progress)
		{
			return Convert(target, new List<ITransformation>(), new List<ITransformation>(), progress);
		}

		public static IEnumerable<Definition.Project> Convert(
			string target,
			ConversionOptions conversionOptions,
			IProgress<string> progress)
		{
			return Convert(target, conversionOptions, new List<ITransformation>(), new List<ITransformation>(), progress);
		}

		public static IEnumerable<Definition.Project> Convert(
			string target,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress
		)
		{
			return Convert(target, new ConversionOptions(), preTransforms, postTransforms, progress);
		}

		public static IEnumerable<Definition.Project> Convert(
			string target,
			ConversionOptions conversionOptions,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress
		)
		{
			if (Path.GetExtension(target).Equals(".sln", StringComparison.OrdinalIgnoreCase))
			{
				progress.Report("Solution parsing started.");
				using (var reader = new StreamReader(File.OpenRead(target)))
				{
					var file = new FileInfo(target);
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						if (!line.Trim().StartsWith("Project("))
						{
							continue;
						}

						var projectPath = line.Split('"').FirstOrDefault(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
						if (projectPath != null)
						{
							progress.Report("Project found: " + projectPath);
							var fullPath = Path.Combine(file.Directory.FullName, projectPath);
							if (!File.Exists(fullPath))
							{
								progress.Report("Project file not found at: " + fullPath);
							}
							else
							{
								yield return ProcessFile(fullPath, conversionOptions, preTransforms, postTransforms, progress);
							}
						}
					}
				}

				yield break;
			}

			// Process all csprojs found in given directory
			if (!Path.GetExtension(target).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
			{
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
					yield return ProcessFile(projectFile, conversionOptions, preTransforms, postTransforms, progress);
				}

				yield break;
			}

			// Process only the given project file
			yield return ProcessFile(target, conversionOptions, preTransforms, postTransforms, progress);
		}

		private static Project ProcessFile(string filePath,
			ConversionOptions conversionOptions,
			IReadOnlyList<ITransformation> preTransforms,
			IReadOnlyList<ITransformation> postTransforms,
			IProgress<string> progress)
		{
			var file = new FileInfo(filePath);
			if (!Validate(file, progress))
			{
				return null;
			}

			var project = new ProjectReader().Read(filePath, progress);
			if (project == null)
			{
				return null;
			}

			foreach (var transform in preTransforms)
			{
				transform.Transform(project, progress);
			}

			foreach (var transform in TransformationsToApply(conversionOptions))
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
			if (!file.Exists)
			{
				progress.Report($"File {file.FullName} could not be found.");
				return false;
			}

			return true;
		}
	}
}
