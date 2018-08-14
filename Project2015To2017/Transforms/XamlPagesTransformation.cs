using Project2015To2017.Definition;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Transforms
{
	public sealed class XamlPagesTransformation : ITransformation
	{
		/// <inheritdoc />
		public void Transform(Project definition, IProgress<string> progress)
		{
			if (!definition.IsWindowsPresentationFoundationProject)
			{
				return;
			}

			var removeQueue = definition.IncludeItems.Where(x => XamlPageFilter(x, definition)).ToList();

			if (removeQueue.Count == 0)
			{
				return;
			}

			progress.Report($"Removed {removeQueue.Count} XAML items thanks to MSBuild.Sdk.Extras defaults");

			definition.IncludeItems = definition.IncludeItems.Except(removeQueue).ToArray();
		}

		private static readonly string[] FilteredTags = { "Page", "ApplicationDefinition", "Compile", "None" };

		private static bool XamlPageFilter(XElement x, Project definition)
		{
			var tagLocalName = x.Name.LocalName;
			if (!FilteredTags.Contains(tagLocalName))
			{
				return false;
			}

			var include = x.Attribute("Include")?.Value;
			var update = x.Attribute("Update")?.Value;
			var link = include ?? update;

			if (link == null)
			{
				return false;
			}

			var projectFolder = definition.ProjectFolder.FullName;
			var inProject = Path.GetFullPath(Path.Combine(projectFolder, link)).StartsWith(projectFolder);
			if (!inProject)
			{
				return false;
			}

			var fileName = Path.GetFileName(link);

			if (update != null)
			{
				if (tagLocalName == "Compile")
				{
					if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
					{
						var autoGen = x.Element("AutoGen")?.Value ?? "true";
						if (!string.Equals(autoGen, "true", StringComparison.OrdinalIgnoreCase))
						{
							return false;
						}

						var designTime = x.Element("DesignTime")?.Value ?? "true";
						if (!string.Equals(designTime, "true", StringComparison.OrdinalIgnoreCase))
						{
							return false;
						}

						var designTimeSharedInput = x.Element("DesignTimeSharedInput")?.Value ?? "true";
						if (!string.Equals(designTimeSharedInput, "true", StringComparison.OrdinalIgnoreCase))
						{
							return false;
						}

						return true;
					}

					return update.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
					       && x.Descendants().All(c => c.Name.LocalName == "DependentUpon");
				}

				return false;
			}

			if (tagLocalName == "None" && fileName.EndsWith(".settings", StringComparison.OrdinalIgnoreCase))
			{
				var generator = x.Element("Generator")?.Value ?? "SettingsSingleFileGenerator";
				var lastGenOutput = x.Element("LastGenOutput")?.Value ?? ".cs";

				return string.Equals(generator, "SettingsSingleFileGenerator")
					   && lastGenOutput.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
			}

			return include.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);
		}
	}
}