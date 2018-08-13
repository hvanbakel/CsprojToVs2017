using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public static class ProjectPropertiesReader
	{
		public static void PopulateProperties(Project project, XDocument projectXml)
		{
			var propertyGroups = projectXml.Element(project.XmlNamespace + "Project")
				.Elements(project.XmlNamespace + "PropertyGroup")
				.ToArray();

			var unconditionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") == null).ToArray();
			if (unconditionalPropertyGroups.Length == 0)
			{
				throw new NotSupportedException(
					"No unconditional property group found. Cannot determine important properties like target framework and others.");
			}

			project.RootNamespace = unconditionalPropertyGroups.Elements(project.XmlNamespace + "RootNamespace")
				.FirstOrDefault()?.Value;
			project.AssemblyName = unconditionalPropertyGroups.Elements(project.XmlNamespace + "AssemblyName")
				.FirstOrDefault()
				?.Value;

			var targetFrameworkVersion = unconditionalPropertyGroups
				.Elements(project.XmlNamespace + "TargetFrameworkVersion")
				.FirstOrDefault();
			if (targetFrameworkVersion?.Value != null)
			{
				project.TargetFrameworks.Add(ToTargetFramework(targetFrameworkVersion.Value));
				targetFrameworkVersion.Remove();
			}
			else
			{
				var targetFrameworks = unconditionalPropertyGroups
					.Elements(project.XmlNamespace + "TargetFrameworks")
					.FirstOrDefault()
					?.Value;
				if (targetFrameworks != null)
				{
					foreach (var framework in targetFrameworks.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
					{
						project.TargetFrameworks.Add(framework);
					}
				}
				else
				{
					var targetFramework = unconditionalPropertyGroups
						.Elements(project.XmlNamespace + "TargetFramework")
						.FirstOrDefault()
						?.Value;
					if (targetFramework != null)
					{
						project.TargetFrameworks.Add(targetFramework);
					}
				}
			}

			// Ref.: https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
			if (unconditionalPropertyGroups.Elements(project.XmlNamespace + "TestProjectType").Any() ||
			    unconditionalPropertyGroups.Elements(project.XmlNamespace + "ProjectTypeGuids").Any(e =>
				    e.Value.IndexOf("3AC096D0-A1C2-E12C-1390-A8335801FDAB", StringComparison.OrdinalIgnoreCase) > -1))
			{
				project.Type = ApplicationType.TestProject;
			}
			else
			{
				project.Type = ToApplicationType(
					unconditionalPropertyGroups.Elements(project.XmlNamespace + "OutputType").FirstOrDefault()?.Value ??
					propertyGroups.Elements(project.XmlNamespace + "OutputType").FirstOrDefault()?.Value ??
					(project.IsModernProject ? "library" : null));
			}

			(project.Configurations, project.Platforms) = ReadConditionals(unconditionalPropertyGroups, project);

			project.BuildEvents = propertyGroups.Elements().Where(x =>
					x.Name == project.XmlNamespace + "PostBuildEvent" ||
					x.Name == project.XmlNamespace + "PreBuildEvent")
				.ToArray();
			project.AdditionalPropertyGroups = ReadAdditionalPropertyGroups(project, propertyGroups);

			project.Imports = projectXml.Element(project.XmlNamespace + "Project")
				.Elements(project.XmlNamespace + "Import").Where(x =>
					x.Attribute("Project")?.Value != @"$(MSBuildToolsPath)\Microsoft.CSharp.targets" &&
					x.Attribute("Project")?.Value != @"$(MSBuildBinPath)\Microsoft.CSharp.targets" &&
					x.Attribute("Project")?.Value !=
					@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props").ToArray();

			project.Targets = projectXml.Element(project.XmlNamespace + "Project")
				.Elements(project.XmlNamespace + "Target").ToArray();

			if (project.Type == ApplicationType.Unknown)
			{
				throw new NotSupportedException("Unable to parse output type.");
			}
		}

		private static (List<string>, List<string>) ReadConditionals(XElement[] unconditionalPropertyGroups,
			Project project)
		{
			var projectXml = project.ProjectDocument;

			var configurationSet = new HashSet<string>();
			var platformSet = new HashSet<string>();

			var configurationsFromProperty = ParseFromProperty("Configurations");
			var platformsFromProperty = ParseFromProperty("Platforms");

			var needConfigurations = configurationsFromProperty == null;
			if (!needConfigurations)
			{
				foreach (var configuration in configurationsFromProperty)
				{
					configurationSet.Add(configuration);
				}
			}

			var needPlatforms = platformsFromProperty == null;
			if (!needPlatforms)
			{
				foreach (var platform in platformsFromProperty)
				{
					platformSet.Add(platform);
				}
			}

			if (project.IsModernProject)
			{
				if (needConfigurations)
				{
					configurationSet.Add("Debug");
					configurationSet.Add("Release");
					needConfigurations = false;
				}

				if (needPlatforms)
				{
					platformSet.Add("AnyCPU");
					needPlatforms = false;
				}
			}

			if (needConfigurations || needPlatforms)
			{
				foreach (var x in projectXml.Descendants())
				{
					var condition = x.Attribute("Condition");
					if (condition == null) continue;

					var conditionValue = condition.Value;
					if (!conditionValue.Contains("$(Configuration)") &&
					    !conditionValue.Contains("$(Platform)")) continue;

					var conditionEvaluated = ConditionEvaluator.GetConditionValues(conditionValue);

					if (needConfigurations && conditionEvaluated.TryGetValue("Configuration", out var configurations))
					{
						foreach (var configuration in configurations)
						{
							configurationSet.Add(configuration);
						}
					}

					if (needPlatforms && conditionEvaluated.TryGetValue("Platform", out var platforms))
					{
						foreach (var platform in platforms)
						{
							platformSet.Add(platform);
						}
					}
				}
			}

			var configurationList = configurationSet.ToList();
			var platformList = platformSet.ToList();
			configurationList.Sort();
			platformList.Sort();
			return (configurationList, platformList);

			string[] ParseFromProperty(string name) => unconditionalPropertyGroups.Elements(project.XmlNamespace + name)
				.FirstOrDefault()
				?.Value
				.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
		}

		private static List<XElement> ReadAdditionalPropertyGroups(Project project, XElement[] propertyGroups)
		{
			var additionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") != null).ToList();
			var otherElementsInPropertyGroupWithNoCondition = propertyGroups
				.Where(x => x.Attribute("Condition") == null)
				.SelectMany(x => x.Elements());

			additionalPropertyGroups.Add(new XElement("PropertyGroup",
				otherElementsInPropertyGroupWithNoCondition.ToArray()));

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