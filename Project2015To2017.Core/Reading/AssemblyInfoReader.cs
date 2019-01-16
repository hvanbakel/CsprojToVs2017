using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public sealed class AssemblyInfoReader
	{
		private readonly ILogger logger;

		public AssemblyInfoReader(ILogger logger)
		{
			this.logger = logger;
		}

		public AssemblyAttributes Read(Project project)
		{
			var projectPath = project.ProjectFolder.FullName;

			var (compileItems, wildcardCompileItems) = project.ItemGroups
				.SelectMany(x => x.Descendants(project.XmlNamespace + "Compile"))
				.Attributes("Include")
				.Select(x => x.Value.ToString())
				.Split(x => !x.Contains("*"));

			var allFiles = compileItems
				.Select(x =>
					{
						var filePath = Path.IsPathRooted(x) ? x : Path.GetFullPath(Path.Combine(projectPath, x));
						return new FileInfo(Extensions.MaybeAdjustFilePath(filePath, projectPath));
					}
				);

			if (project.IsModernProject || wildcardCompileItems.Count > 0)
				allFiles = allFiles.Concat(project.FindAllWildcardFiles(project.CodeFileExtension));

			var assemblyInfoAllFiles = allFiles
				.Where(x => IsAssemblyInfoFile(x, project.CodeFileExtension))
				.ToList();

			if (assemblyInfoAllFiles.Count == 0)
			{
				// for modern projects an assembly info is not required.
				if (!project.IsModernProject)
				{
					this.logger.LogWarning("Could not read assembly information, no such file found");
				}

				return null;
			}

			var rootDirectory = project.TryFindBestRootDirectory();
			var (assemblyInfoFiles, assemblyInfoMissingFiles) = assemblyInfoAllFiles.Split(x => x.Exists);

			foreach (var assemblyInfoMissingFile in assemblyInfoMissingFiles)
			{
				this.logger.LogWarning(
					$@"Assembly information file '{rootDirectory.GetRelativePathTo(assemblyInfoMissingFile)}' not found");

				if (assemblyInfoAllFiles.Count == 1)
					return null;
			}

			if (assemblyInfoAllFiles.Count > 1)
			{
				var fileList = string.Join(", ", assemblyInfoAllFiles.Select(x => rootDirectory.GetRelativePathTo(x)));
				this.logger.LogWarning(
					$@"Could not read assembly information, multiple files found:{Environment.NewLine}{fileList}");

				project.HasMultipleAssemblyInfoFiles = true;
				return null;
			}

			var assemblyInfoFile = assemblyInfoFiles[0];
			var assemblyInfoFileName = assemblyInfoFile.FullName;

			this.logger.LogDebug($"Reading assembly information from {assemblyInfoFileName}.");

			var text = File.ReadAllText(assemblyInfoFileName);

			var tree = CSharpSyntaxTree.ParseText(text);

			var root = (CompilationUnitSyntax) tree.GetRoot();

			var assemblyAttributes = new AssemblyAttributes
			{
				File = assemblyInfoFile,
				FileContents = root
			};

			return assemblyAttributes;
		}

		private static bool IsAssemblyInfoFile(FileInfo x, string extension)
		{
			var nameLower = x.Name.ToLower();
			if (nameLower == "assemblyinfo." + extension)
				return true;
			return nameLower.EndsWith("." + extension) && nameLower.Contains("assemblyinfo");
		}
	}
}