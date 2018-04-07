using System;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

using static Project2015To2017.Definition.Project;

namespace Project2015To2017.Reading
{
	public static class ProjectPropertiesReader
	{
		public static void PopulateProperties(ProjectBuilder definition, XDocument projectXml)
		{
			var propertyGroups = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "PropertyGroup");

			var unconditionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") == null).ToArray();
			if (unconditionalPropertyGroups.Length == 0)
			{
				throw new NotSupportedException("No unconditional property group found. Cannot determine important properties like target framework and others.");
			}
			else
			{
				var targetFramework = unconditionalPropertyGroups.Elements(XmlNamespace + "TargetFrameworkVersion").FirstOrDefault()?.Value;

				definition.Optimize = "true".Equals(unconditionalPropertyGroups.Elements(XmlNamespace + "Optimize").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
				definition.TreatWarningsAsErrors = "true".Equals(unconditionalPropertyGroups.Elements(XmlNamespace + "TreatWarningsAsErrors").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
				definition.AllowUnsafeBlocks = "true".Equals(unconditionalPropertyGroups.Elements(XmlNamespace + "AllowUnsafeBlocks").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);

				definition.RootNamespace = unconditionalPropertyGroups.Elements(XmlNamespace + "RootNamespace").FirstOrDefault()?.Value;
				definition.AssemblyName = unconditionalPropertyGroups.Elements(XmlNamespace + "AssemblyName").FirstOrDefault()?.Value;

				definition.SignAssembly = "true".Equals(unconditionalPropertyGroups.Elements(XmlNamespace + "SignAssembly").FirstOrDefault()?.Value, StringComparison.OrdinalIgnoreCase);
				definition.AssemblyOriginatorKeyFile = unconditionalPropertyGroups.Elements(XmlNamespace + "AssemblyOriginatorKeyFile").FirstOrDefault()?.Value;

				// Ref.: https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
				if (unconditionalPropertyGroups.Elements(XmlNamespace + "TestProjectType").Any() ||
					unconditionalPropertyGroups.Elements(XmlNamespace + "ProjectTypeGuids").Any(e => e.Value.IndexOf("3AC096D0-A1C2-E12C-1390-A8335801FDAB", StringComparison.OrdinalIgnoreCase) > -1))
				{
					definition.Type = ApplicationType.TestProject;
				}
				else
				{
					definition.Type = ToApplicationType(unconditionalPropertyGroups.Elements(XmlNamespace + "OutputType").FirstOrDefault()?.Value ??
						propertyGroups.Elements(XmlNamespace + "OutputType").FirstOrDefault()?.Value);
				}

				if (targetFramework != null)
				{
					definition.TargetFrameworks = new[] { ToTargetFramework(targetFramework) };
				}
			}

			// yuk... but hey it works...
			definition.Configurations = propertyGroups
				.Where(x => x.Attribute("Condition") != null)
				.Select(x => x.Attribute("Condition").Value)
				.Where(x => x.Contains("'$(Configuration)|$(Platform)'"))
				.Select(x => x.Split('\'').Skip(3).FirstOrDefault())
				.Where(x => x != null)
				.Select(x => x.Split('|')[0])
				.ToArray();

			definition.BuildEvents = propertyGroups.Elements().Where(x => x.Name == XmlNamespace + "PostBuildEvent" || x.Name == XmlNamespace + "PreBuildEvent").ToArray();
			definition.AdditionalPropertyGroups = ReadAdditionalPropertyGroups(propertyGroups);

			definition.Imports = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "Import").Where(x =>
					x.Attribute("Project")?.Value != @"$(MSBuildToolsPath)\Microsoft.CSharp.targets" &&
					x.Attribute("Project")?.Value != @"$(MSBuildBinPath)\Microsoft.CSharp.targets" &&
					x.Attribute("Project")?.Value != @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props").ToArray();

			definition.Targets = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "Target").ToArray();

			if (definition.Type == ApplicationType.Unknown)
			{
				throw new NotSupportedException("Unable to parse output type.");
			}
		}

		private static System.Collections.Generic.IReadOnlyList<XElement> ReadAdditionalPropertyGroups(System.Collections.Generic.IEnumerable<XElement> propertyGroups)
		{
			var additionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") != null).ToList();
			var versionControlElements = propertyGroups
				.SelectMany(x => x.Elements().Where(e => e.Name.LocalName.StartsWith("Scc")))
				.ToArray();
			if (versionControlElements != null)
			{
				additionalPropertyGroups.Add(new XElement("PropertyGroup", versionControlElements));
			}

			return additionalPropertyGroups;
		}

		private static string ToTargetFramework(string targetFramework)
		{
			if (targetFramework.StartsWith("v", StringComparison.OrdinalIgnoreCase))
			{
				return "net" + targetFramework.Substring(1).Replace(".", string.Empty);
			}

			throw new NotSupportedException($"Target framework {targetFramework} is not supported.");
		}

		private static ApplicationType ToApplicationType(string outputType)
		{
			if (string.IsNullOrWhiteSpace(outputType))
			{
				return ApplicationType.Unknown;
			}

			switch (outputType.ToLowerInvariant())
			{
				case "exe": return ApplicationType.ConsoleApplication;
				case "library": return ApplicationType.ClassLibrary;
				case "winexe": return ApplicationType.WindowsApplication;
				default: throw new NotSupportedException($"OutputType {outputType} is not supported.");
			}
		}
	}
}