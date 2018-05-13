using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class AssemblyInfoReader
	{
		public AssemblyAttributes Read(
			FileInfo projectFile, IProgress<string> progress
		)
		{
			var projectFolder = projectFile.Directory;

			var assemblyInfoFiles = projectFolder
										.EnumerateFiles("AssemblyInfo.cs", SearchOption.AllDirectories)
										.ToArray();

			if (assemblyInfoFiles.Length == 1)
			{
				progress.Report($"Reading assembly info from {assemblyInfoFiles[0].FullName}.");

				var text = File.ReadAllText(assemblyInfoFiles[0].FullName);

				var tree = CSharpSyntaxTree.ParseText(text);
 
				var root = (CompilationUnitSyntax)tree.GetRoot();

				var assemblyAttributes = new AssemblyAttributes
				{
					FileContents = root
				};

				var title = assemblyAttributes.Title;

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