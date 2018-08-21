using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class PrimaryUnconditionalPropertyTransformation : ITransformation
	{
		public void Transform(Project project, ILogger logger)
		{
			var propertyGroup = project.PrimaryPropertyGroup;

			AddTargetFrameworks(propertyGroup, project.TargetFrameworks);

			var configurations = project.Configurations ?? Array.Empty<string>();
			if (configurations.Count != 0)
				// ignore default "Debug;Release" configuration set
				if (configurations.Count != 2 || !configurations.Contains("Debug") ||
				    !configurations.Contains("Release"))
					AddIfNotNull(propertyGroup, "Configurations", string.Join(";", configurations));

			var platforms = project.Platforms ?? Array.Empty<string>();
			if (platforms.Count != 0)
				// ignore default "AnyCPU" platform set
				if (platforms.Count != 1 || !platforms.Contains("AnyCPU"))
					AddIfNotNull(propertyGroup, "Platforms", string.Join(";", platforms));

			var outputProjectName = Path.GetFileNameWithoutExtension(project.FilePath.Name);

			AddIfNotNull(propertyGroup, "RootNamespace",
				project.RootNamespace != outputProjectName ? project.RootNamespace : null);
			AddIfNotNull(propertyGroup, "AssemblyName",
				project.AssemblyName != outputProjectName ? project.AssemblyName : null);
			AddIfNotNull(propertyGroup, "AppendTargetFrameworkToOutputPath",
				project.AppendTargetFrameworkToOutputPath ? null : "false");

			AddIfNotNull(propertyGroup, "ExtrasEnableWpfProjectSetup",
				project.IsWindowsPresentationFoundationProject ? "true" : null);
			AddIfNotNull(propertyGroup, "ExtrasEnableWinFormsProjectSetup",
				project.IsWindowsFormsProject ? "true" : null);

			string outputType;
			switch (project.Type)
			{
				case ApplicationType.ConsoleApplication:
					outputType = "Exe";
					break;
				case ApplicationType.WindowsApplication:
					outputType = "WinExe";
					break;
				default:
					outputType = null;
					break;
			}
			AddIfNotNull(propertyGroup, "OutputType", outputType);

			propertyGroup.Add(project.AssemblyAttributeProperties);

			AddPackageNodes(propertyGroup, project.PackageConfiguration);

			if (project.BuildEvents != null && project.BuildEvents.Any())
			{
				foreach (var buildEvent in project.BuildEvents.Select(ExtensionMethods.RemoveAllNamespaces))
				{
					propertyGroup.Add(buildEvent);
				}
			}
		}

		private void AddPackageNodes(XElement mainPropertyGroup, PackageConfiguration packageConfiguration)
		{
			if (packageConfiguration == null)
			{
				return;
			}

			//Add those properties not already covered by the project properties

			AddIfNotNull(mainPropertyGroup, "Authors", packageConfiguration.Authors);
			AddIfNotNull(mainPropertyGroup, "PackageIconUrl", packageConfiguration.IconUrl);
			AddIfNotNull(mainPropertyGroup, "PackageId", packageConfiguration.Id);
			AddIfNotNull(mainPropertyGroup, "PackageLicenseUrl", packageConfiguration.LicenseUrl);
			AddIfNotNull(mainPropertyGroup, "PackageProjectUrl", packageConfiguration.ProjectUrl);
			AddIfNotNull(mainPropertyGroup, "PackageReleaseNotes", packageConfiguration.ReleaseNotes);
			AddIfNotNull(mainPropertyGroup, "PackageTags", packageConfiguration.Tags);

			if (packageConfiguration.Id != null && packageConfiguration.Tags == null)
				mainPropertyGroup.Add(new XElement("PackageTags", "Library"));

			if (packageConfiguration.RequiresLicenseAcceptance)
			{
				mainPropertyGroup.Add(new XElement("PackageRequireLicenseAcceptance", "true"));
			}
		}

		private static void AddIfNotNull(XElement node, string elementName, string value)
		{
			XElement newElement = null;
			if (!string.IsNullOrWhiteSpace(value))
			{
				newElement = new XElement(elementName, value);
			}

			ReplaceAnyWith(newElement, node, elementName);
		}

		private void AddTargetFrameworks(XElement mainPropertyGroup, IList<string> targetFrameworks)
		{
			if (targetFrameworks == null || targetFrameworks.Count == 0)
			{
				return;
			}

			var newElement = targetFrameworks.Count > 1
				? new XElement("TargetFrameworks", string.Join(";", targetFrameworks))
				: new XElement("TargetFramework", targetFrameworks[0]);

			ReplaceAnyWith(newElement, mainPropertyGroup,
				"TargetFrameworks", "TargetFramework", "TargetFrameworkVersion");
		}

		private static (XElement, IReadOnlyCollection<XElement>) FindExistingElements(XElement node,
			params string[] names)
		{
			XElement bestMatch = null;
			var other = new List<XElement>();

			foreach (var name in names)
			{
				foreach (var child in node.ElementsAnyNamespace(name))
				{
					if (bestMatch == null)
					{
						bestMatch = child;
						continue;
					}

					other.Add(child);
				}
			}

			return (bestMatch, other);
		}

		private static void ReplaceAnyWith(XElement newElement, XElement parent, params string[] names)
		{
			var (existingElement, others) = FindExistingElements(parent, names);

			if (newElement != null)
			{
				if (existingElement == null)
				{
					parent.Add(newElement);
				}
				else
				{
					existingElement.ReplaceWith(newElement);
				}
			}
			else
			{
				existingElement?.Remove();
			}

			foreach (var child in others)
			{
				child.Remove();
			}
		}
	}
}