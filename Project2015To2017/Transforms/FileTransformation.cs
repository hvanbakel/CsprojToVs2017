using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class FileTransformation : ILegacyOnlyProjectTransformation
	{
		private static readonly IReadOnlyCollection<string> ItemsToProjectChecked = new[]
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
			"EmbeddedResource",
		};

		private static readonly IReadOnlyCollection<string> ItemsToProjectAlways = new[]
		{
			"Reference",
			"ProjectReference",
			"PackageReference",
		};

		private readonly ILogger logger;

		public FileTransformation(ILogger logger = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
		}

		public void Transform(Project definition)
		{
			var (keepItems, removeQueue) = definition.ItemGroups
				.SelectMany(x => x.Elements())
				.Split(x => KeepFileInclusion(x, definition));

			// For all retained Page, Content, etc that have .cs extension we get file paths.
			// For all these paths we add <Compile Remove="(path)" />.
			// So that there is no wildcard match like <Compile Include="**/*.cs" /> for file test.cs,
			// already included as (e.g.) Content: <Content Include="test.cs" />
			var otherIncludeFilesMatchingWildcard = keepItems
				.Where(x => x.Name.LocalName != "Compile")
				.Select(x => x.Attribute("Include")?.Value)
				.Where(x => !string.IsNullOrEmpty(x))
				.Where(x => x.EndsWith("." + definition.CodeFileExtension, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			if (otherIncludeFilesMatchingWildcard.Length > 0)
			{
				var itemGroup = new XElement(definition.XmlNamespace + "ItemGroup");
				foreach (var otherIncludeMatchingWildcard in otherIncludeFilesMatchingWildcard)
				{
					var removeOtherInclude = new XElement(definition.XmlNamespace + "Compile");
					removeOtherInclude.Add(new XAttribute("Remove", otherIncludeMatchingWildcard));
					itemGroup.Add(removeOtherInclude);
				}

				definition.ItemGroups.Add(itemGroup);
			}

			var count = 0u;

			foreach (var x in removeQueue)
			{
				x.Remove();
				count++;
			}

			if (count == 0)
			{
				return;
			}

			logger.LogDebug($"Removed {count} include items thanks to Microsoft.NET.Sdk defaults");
		}

		private static bool KeepFileInclusion(XElement x, Project project)
		{
			var tagName = x.Name.LocalName;
			var include = x.Attribute("Include")?.Value;

			// Wildcards from Microsoft.NET.Sdk.DefaultItems.props
			switch (tagName)
			{
				case "Compile":
				case "EmbeddedResource"
					when include != null && include.EndsWith(".resx", StringComparison.OrdinalIgnoreCase):
					return !IsWildcardMatchedFile(project, x);
			}

			if (ItemsToProjectAlways.Contains(tagName))
			{
				return true;
			}

			// Visual Studio Test Projects
			if (tagName == "Service" && string.Equals(include,
				    "{82a7f48d-3b50-4b1e-b82e-3ada8210c358}", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (!ItemsToProjectChecked.Contains(tagName))
			{
				return false;
			}

			if (include == null)
			{
				return true;
			}

			// Remove packages.config since those references were already added to the CSProj file.
			if (include.Equals("packages.config", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// Nuspec is no longer required
			if (include.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (tagName == "None" && include.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			return true;
		}

		private static bool IsWildcardMatchedFile(
			Project project,
			XElement compiledFile)
		{
			var filePath = compiledFile.Attribute("Include")?.Value;
			if (filePath == null)
			{
				return false;
			}

			if (filePath.Contains("*"))
			{
				return false;
			}

			var compiledFileAttributes = compiledFile.Attributes().Where(a => a.Name != "Include").ToList();

			// keep Link as an Include
			var linkElement = compiledFile.Elements().FirstOrDefault(a => a.Name.LocalName == "Link");
			if (linkElement != null)
			{
				compiledFileAttributes.Add(new XAttribute("Include", filePath));
				compiledFileAttributes.Add(new XAttribute("Link", linkElement.Value));
				linkElement.Remove();

				compiledFile.ReplaceAttributes(compiledFileAttributes);
			}

			var projectFolder = project.ProjectFolder.FullName;

			if (!Path.GetFullPath(Path.Combine(projectFolder, filePath)).StartsWith(projectFolder))
			{
				return false;
			}

			if (linkElement == null)
			{
				compiledFileAttributes.Add(new XAttribute("Update", filePath));
				compiledFile.ReplaceAttributes(compiledFileAttributes);
			}

			if (compiledFile.Attributes().Count() != 1)
			{
				return false;
			}

			if (compiledFile.Elements().Count() != 0)
			{
				switch (compiledFile.Name.LocalName)
				{
					case "Compile":
						return CompileChildrenVerification(compiledFile);
					case "EmbeddedResource":
						return EmbeddedResourceChildrenVerification(compiledFile);
				}
			}

			return true;
		}

		private static bool CompileChildrenVerification(XElement item)
		{
			// retain only if it is not <SubType>Code</SubType>
			if (!Extensions.ValidateChildren(item.Elements(), "SubType"))
			{
				return false;
			}

			var subType = item.Element(item.Name.Namespace + "SubType")?.Value;
			return subType == "Code";
		}

		private static bool EmbeddedResourceChildrenVerification(XElement item)
		{
			var nameSet = new HashSet<string>(item.Elements().Select(x => x.Name.LocalName));

			if (nameSet.Contains("Generator"))
			{
				var value = item.Element(item.Name.Namespace + "Generator")?.Value;
				if (value != "ResXFileCodeGenerator")
				{
					return false;
				}

				nameSet.Remove("Generator");
			}

			nameSet.Remove("LastGenOutput");

			return nameSet.Count == 0;
		}
	}
}