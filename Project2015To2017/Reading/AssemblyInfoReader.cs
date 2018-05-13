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