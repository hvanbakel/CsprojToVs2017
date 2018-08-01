using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class AssemblyInfoReader
	{
		public AssemblyAttributes Read(Project project, IProgress<string> progress)
		{
			var projectPath = project.ProjectFolder.FullName;

			var compileElements = project.IncludeItems
										 .SelectMany(x => x.Elements(project.XmlNamespace + "Compile"))
										 .ToList();

			var assemblyInfoFiles = compileElements
										   .Attributes("Include")
										   .Select(x => x.Value.ToString())
										   .Where(x => !x.Contains("*"))
										   .Select(x =>
												{
													var filePath = Path.IsPathRooted(x) ? x : Path.GetFullPath(Path.Combine(projectPath, x));
													return new FileInfo(filePath);
												}
											)
										   .Where(IsAssemblyInfoFile)
										   .Where(x =>
												{
													if (x.Exists)
													{
														return true;
													}
													progress.Report($@"AssemblyInfo file '{x.FullName}' not found");
													return false;
												}
											)
										   .ToList();

			if (assemblyInfoFiles.Count == 0)
			{
				progress.Report($@"Could not read from assemblyinfo, no assemblyinfo file found");

				return null;
			}

			if (assemblyInfoFiles.Count > 1)
			{
				var fileList = string.Join($",{Environment.NewLine}", assemblyInfoFiles.Select(x => x.FullName));
				progress.Report($@"Could not read from assemblyinfo, multiple assemblyinfo files found:{Environment.NewLine}{fileList}");

				project.HasMultipleAssemblyInfoFiles = true;
				return null;
			}

			var assemblyInfoFile = assemblyInfoFiles[0];
			var assemblyInfoFileName = assemblyInfoFile.FullName;

			progress.Report($"Reading assembly info from {assemblyInfoFileName}.");

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

		private static bool IsAssemblyInfoFile(FileInfo x)
		{
			var nameLower = x.Name.ToLower();
			if (nameLower == "assemblyinfo.cs")
				return true;
			return nameLower.EndsWith(".cs") && nameLower.Contains("assemblyinfo");
		}
	}
}