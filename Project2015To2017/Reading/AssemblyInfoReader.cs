using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Project2015To2017.Definition.Project;

using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class AssemblyInfoReader
	{
		public AssemblyAttributes Read(
			Project project, IProgress<string> progress
		)
		{
			var projectPath = project.ProjectFolder.FullName;

			var compileElements = project.IncludeItems
										 .SelectMany(x => x.Elements(XmlNamespace + "Compile"))
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
										   .Where(x => x.Name.ToLower() == "assemblyinfo.cs")
										   .ToList();

			if (assemblyInfoFiles.Count == 1)
			{
				var assemblyInfoFile = assemblyInfoFiles[0];
				var assemblyInfoFileName = assemblyInfoFile.FullName;

				progress.Report($"Reading assembly info from {assemblyInfoFileName}.");

				var text = File.ReadAllText(assemblyInfoFileName);

				var tree = CSharpSyntaxTree.ParseText(text);

				var root = (CompilationUnitSyntax)tree.GetRoot();

				var assemblyAttributes = new AssemblyAttributes
				{
					File = assemblyInfoFile,
					FileContents = root
				};

				return assemblyAttributes;
			}
			else
			{
				progress.Report($@"Could not read from assemblyinfo, multiple assemblyinfo files found: 
{string.Join(Environment.NewLine, assemblyInfoFiles.Select(x => x.FullName))}.");
			}

			return null;
		}
	}
}