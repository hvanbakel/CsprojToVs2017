using System;
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

			var compileElements = project.ItemGroups
										 .SelectMany(x => x.Descendants(project.XmlNamespace + "Compile"))
										 .ToList();

			var missingCount = 0u;

			var assemblyInfoFiles = compileElements
										   .Attributes("Include")
										   .Select(x => x.Value.ToString())
										   .Where(x => !x.Contains("*"))
										   .Select(x =>
												{
													var filePath = Path.IsPathRooted(x) ? x : Path.GetFullPath(Path.Combine(projectPath, x));
													return new FileInfo(Extensions.MaybeAdjustFilePath(filePath, projectPath));
												}
											)
										   .Where(x => IsAssemblyInfoFile(x, project.CodeFileExtension))
										   .Where(x =>
												{
													if (x.Exists)
													{
														return true;
													}

													missingCount++;
													this.logger.LogWarning($@"AssemblyInfo file '{x.FullName}' not found");
													return false;
												}
											)
										   .ToList();

			if (assemblyInfoFiles.Count == 0)
			{
				this.logger.LogWarning($@"Could not read from assemblyinfo, no assemblyinfo file found");

				return null;
			}

			if (assemblyInfoFiles.Count + missingCount > 1)
			{
				var fileList = string.Join($",{Environment.NewLine}", assemblyInfoFiles.Select(x => x.FullName));
				this.logger.LogWarning($@"Could not read from assemblyinfo, multiple assemblyinfo files found:{Environment.NewLine}{fileList}");

				project.HasMultipleAssemblyInfoFiles = true;
				return null;
			}

			var assemblyInfoFile = assemblyInfoFiles[0];
			var assemblyInfoFileName = assemblyInfoFile.FullName;

			this.logger.LogInformation($"Reading assembly info from {assemblyInfoFileName}.");

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