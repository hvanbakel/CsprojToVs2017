using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;
using static Project2015To2017.Definition.Project;

namespace Project2015To2017.Transforms
{
	internal sealed class FileTransformation : ITransformation
	{
		private static readonly IReadOnlyList<string> ItemsToProject = new[]
		{
			"None",
			"Content",
			"AdditionalFiles",
			"CodeAnalysisDictionary",
			"ApplicationDefinition",
			"Page",
			"Resource",
			"SplashScreen",
			"DesignData",
			"DesignDataWithDesignTimeCreatableTypes",
			"EntityDeploy",
			"XamlAppDef",
			"EmbeddedResource"
		};

		public void Transform(Project definition, IProgress<string> progress)
		{
			var items = definition.IncludeItems;

			var compileManualIncludes = FindNonWildcardMatchedFiles(definition.ProjectFolder, items, "*.cs", XmlNamespace + "Compile", progress);

			var otherIncludes = ItemsToProject.SelectMany(x => items.Elements(XmlNamespace + x))
											  .Where(KeepFileInclusion);

			var itemsToInclude = compileManualIncludes
									.Concat(otherIncludes)
									.ToList()
									.AsReadOnly();

			definition.IncludeItems = itemsToInclude;
		}

		private static bool KeepFileInclusion(XElement x)
		{
			var include = x.Attribute("Include")?.Value;

			if (include == null)
			{
				return true;
			}

			return
				// Remove packages.config since those references were already added to the CSProj file.
				include != "packages.config" &&
				// Nuspec is no longer required
				!include.EndsWith(".nuspec")
				&&
				//Resource files are added automatically
				!(x.Name == XmlNamespace + "EmbeddedResource"
					&& include.EndsWith(".resx"));
		}

		private static IReadOnlyList<XElement> FindNonWildcardMatchedFiles(
			DirectoryInfo projectFolder,
			IEnumerable<XElement> itemGroups,
			string wildcard,
			XName elementName,
			IProgress<string> progress)
		{
			var manualIncludes = new List<XElement>();
			var filesMatchingWildcard = new List<string>();
			foreach (var compiledFile in itemGroups.Elements(elementName))
			{
				var includeAttribute = compiledFile.Attribute("Include");
				if (includeAttribute != null && !includeAttribute.Value.Contains("*"))
				{
					var compiledFileAttributes = compiledFile.Attributes().Where(a => a.Name != "Include").ToList();
					compiledFileAttributes.Add(new XAttribute("Update", includeAttribute.Value));
					compiledFile.ReplaceAttributes(compiledFileAttributes);

					if (!Path.GetFullPath(Path.Combine(projectFolder.FullName, includeAttribute.Value)).StartsWith(projectFolder.FullName))
					{
						progress.Report("Include cannot be done through wildcard, " +
										$"adding as separate include:{Environment.NewLine}{compiledFile}.");
						manualIncludes.Add(compiledFile);
					}
					else if (compiledFile.Attributes().Count() != 1)
					{
						progress.Report("Include cannot be done exclusively through wildcard, " +
										$"adding as separate update:{Environment.NewLine}{compiledFile}.");
						manualIncludes.Add(compiledFile);
						filesMatchingWildcard.Add(includeAttribute.Value);
					}
					else if (compiledFile.Elements().Count() != 0)
					{
						progress.Report("Include cannot be done exclusively through wildcard, " +
										$"adding as separate update:{Environment.NewLine}{compiledFile}.");

						manualIncludes.Add(compiledFile);

						filesMatchingWildcard.Add(includeAttribute.Value);
					}
					else
					{
						filesMatchingWildcard.Add(includeAttribute.Value);
					}
				}
				else
				{
					progress.Report($"Compile found with no or wildcard include, full node {compiledFile}.");
				}
			}

			var filesInFolder = projectFolder.EnumerateFiles(wildcard, SearchOption.AllDirectories).Select(x => x.FullName.ToUpper()).ToArray();
			var knownFullPaths = manualIncludes
				.Select(x => x.Attribute("Include")?.Value)
				.Where(x => x != null)
				.Concat(filesMatchingWildcard)
				.Select(x => Path.GetFullPath(Path.Combine(projectFolder.FullName, x)).ToUpper())
				.ToArray();

			foreach (var nonListedFile in filesInFolder.Except(knownFullPaths))
			{
				if (nonListedFile.StartsWith(Path.Combine(projectFolder.FullName + "\\obj\\"), StringComparison.OrdinalIgnoreCase))
				{
					// skip the generated files in obj
					continue;
				}

				progress.Report($"File found which was not included, consider removing {nonListedFile}.");
			}

			foreach (var fileNotOnDisk in knownFullPaths.Except(filesInFolder).Where(x => x.StartsWith(projectFolder.FullName, StringComparison.OrdinalIgnoreCase)))
			{
				progress.Report($"File was included but is not on disk: {fileNotOnDisk}.");
			}

			return manualIncludes;
		}
	}
}
