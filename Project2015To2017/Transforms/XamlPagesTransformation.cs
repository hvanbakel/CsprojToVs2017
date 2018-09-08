using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Project2015To2017.Transforms
{
	public sealed class XamlPagesTransformation : ILegacyOnlyProjectTransformation
	{
		public XamlPagesTransformation(ILogger logger = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
		}

		/// <inheritdoc />
		public void Transform(Project definition)
		{
			if (!definition.IsWindowsPresentationFoundationProject)
			{
				return;
			}

			var removeQueue = definition.ItemGroups
				.SelectMany(x => x.Elements())
				.Where(x => XamlPageFilter(x, definition))
				.ToImmutableArray();

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

			logger.LogInformation($"Removed {count} XAML items thanks to MSBuild.Sdk.Extras defaults");
		}

		private static readonly string[] FilteredTags = {"Page", "ApplicationDefinition", "Compile", "None"};
		private readonly ILogger logger;

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

				return link.EndsWith(".xaml.cs", StringComparison.OrdinalIgnoreCase)
				       && x.Descendants().All(VerifyDefaultXamlCompileItem);
			}

			if (update != null)
			{
				return false;
			}

			// from now on (include != null) is invariant

			if (tagLocalName == "None" && fileName.EndsWith(".settings", StringComparison.OrdinalIgnoreCase))
			{
				var generator = x.Element("Generator")?.Value ?? "SettingsSingleFileGenerator";
				var lastGenOutput = x.Element("LastGenOutput")?.Value ?? ".cs";

				return string.Equals(generator, "SettingsSingleFileGenerator")
				       && lastGenOutput.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
			}

			return include.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);

			bool VerifyDefaultXamlCompileItem(XElement child)
			{
				var name = child.Name.LocalName;

				if (child.HasElements)
				{
					return false;
				}

				if (name == "DependentUpon")
				{
					return true;
				}

				return name == "SubType" && string.Equals(child.Value, "Code", StringComparison.OrdinalIgnoreCase);
			}
		}
	}
}