using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

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

			var otherIncludes = ItemsToProject.SelectMany(x => items.Elements(definition.XmlNamespace + x)).Where(
				element => KeepFileInclusion(element, definition));

			var compileManualIncludes = FindNonWildcardMatchedFiles(definition, items, "*.cs", definition.XmlNamespace + "Compile", otherIncludes, progress);

			var itemsToInclude = compileManualIncludes
									.Concat(otherIncludes)
									.ToArray();

			definition.IncludeItems = itemsToInclude;
		}

		private static bool KeepFileInclusion(XElement x, Project project)
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
				!(x.Name == project.XmlNamespace + "EmbeddedResource"
					&& include.EndsWith(".resx"));
		}

		private static IReadOnlyList<XElement> FindNonWildcardMatchedFiles(
			Project project,
			IEnumerable<XElement> itemGroups,
			string wildcard,
			XName elementName,
			IEnumerable<XElement> otherIncludes,
			IProgress<string> progress)
		{
			var projectFolder = project.ProjectFolder;
			var manualIncludes = new List<XElement>();
			var filesMatchingWildcard = new List<string>();
			foreach (var compiledFile in itemGroups.Elements(elementName))
			{
				var includeAttribute = compiledFile.Attribute("Include");
				if (includeAttribute != null && !includeAttribute.Value.Contains("*"))
				{
					var compiledFileAttributes = compiledFile.Attributes().Where(a => a.Name != "Include").ToList();

					//keep Link as an Include
					var linkElement = compiledFile.Elements().FirstOrDefault(a => a.Name.LocalName == "Link");
					if (null != linkElement)
					{
						compiledFileAttributes.Add(new XAttribute("Include", includeAttribute.Value));
						compiledFileAttributes.Add(new XAttribute("Link", linkElement.Value));
						linkElement.Remove();
					}
					else
					{
						compiledFileAttributes.Add(new XAttribute("Update", includeAttribute.Value));
					}

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

						//add only if it is not <SubType>Code</SubType>
						var subType = compiledFile.Elements().FirstOrDefault(x => x.Name.LocalName == "SubType");
						if (subType == null || subType.Value != "Code")
						{
							manualIncludes.Add(compiledFile);
						}

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

			var filesInFolder = projectFolder.EnumerateFiles(wildcard, SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
			var knownFullPaths = manualIncludes
				.Select(x => x.Attribute("Include")?.Value)
				.Where(x => x != null)
				.Concat(filesMatchingWildcard)
				.Select(x => Path.GetFullPath(Path.Combine(projectFolder.FullName, x)))
				.ToList();

			//remove otherIncludes
			var otherIncludeFilesMatchingWildcard = otherIncludes
				.Select(x => x.Attribute("Include")?.Value)
				.Where(x => x != null)
				.Where(x => x.EndsWith(wildcard.TrimStart('*'), StringComparison.OrdinalIgnoreCase))
				.ToArray();

			foreach (var otherIncludeMatchingWildcard in otherIncludeFilesMatchingWildcard)
			{
				var removeOtherInclude = new XElement(project.XmlNamespace + "Compile");
				removeOtherInclude.Add(new XAttribute("Remove", otherIncludeMatchingWildcard));
				manualIncludes.Add(removeOtherInclude);
				
				knownFullPaths.Add(Path.GetFullPath(Path.Combine(projectFolder.FullName, otherIncludeMatchingWildcard)));
			}

			if (!project.IsModernProject)
			{
				foreach (var nonListedFile in filesInFolder.Except(knownFullPaths, StringComparer.OrdinalIgnoreCase))
				{
					if (nonListedFile.StartsWith(Path.Combine(projectFolder.FullName + "\\obj\\"),
						StringComparison.OrdinalIgnoreCase))
					{
						// skip the generated files in obj
						continue;
					}

					progress.Report($"File found which was not included, consider removing {nonListedFile}.");
				}
			}

			foreach (var fileNotOnDisk in knownFullPaths.Except(filesInFolder).Where(x => x.StartsWith(projectFolder.FullName, StringComparison.OrdinalIgnoreCase)))
			{
				progress.Report($"File was included but is not on disk: {fileNotOnDisk}.");
			}

			return manualIncludes;
		}
	}
}
