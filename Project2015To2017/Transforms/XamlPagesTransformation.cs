using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class XamlPagesTransformation : ITransformation
	{
		/// <inheritdoc />
		public void Transform(Project definition, IProgress<string> progress)
		{
			if (!definition.IsWindowsPresentationFoundationProject)
				return;

			var removeQueue = new List<XElement>();
			foreach (var item in definition.IncludeItems.Where(x => XamlPageFilter(x, definition)))
			{
				var path = item.Attribute("Include")?.Value;
				progress.Report($"Removing XAML item thanks to MSBuild.Sdk.Extras defaults: '{path}'");
				removeQueue.Add(item);
			}

			definition.IncludeItems = definition.IncludeItems.Except(removeQueue).ToArray();
		}

		private static bool XamlPageFilter(XElement x, Project definition)
		{
			var tagLocalName = x.Name.LocalName;
			if (tagLocalName != "Page" && tagLocalName != "ApplicationDefinition")
				return false;

			var include = x.Attribute("Include")?.Value;

			if (include == null)
				return false;

			var projectFolder = definition.ProjectFolder.FullName;
			return include.EndsWith(".xaml")
			       && Path.GetFullPath(Path.Combine(projectFolder, include)).StartsWith(projectFolder);
		}
	}
}