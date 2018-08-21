using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Reading
{
	public class ProjectPropertiesReader
	{
		private readonly ILogger _logger;

		private static readonly string[] RemoveMSBuildImports =
		{
			@"$(MSBuildToolsPath)\Microsoft.CSharp.targets",
			@"$(MSBuildBinPath)\Microsoft.CSharp.targets",
			@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props",
		};

		public ProjectPropertiesReader(ILogger logger)
		{
			_logger = logger ?? NoopLogger.Instance;
		}

		public void Read(Project project)
		{
			ReadPropertyGroups(project);

			project.RootNamespace = project.PrimaryPropertyGroup
				.Elements(project.XmlNamespace + "RootNamespace")
				.FirstOrDefault()
				?.Value;

			project.AssemblyName = project.PrimaryPropertyGroup
				.Elements(project.XmlNamespace + "AssemblyName")
				.FirstOrDefault()
				?.Value;

			var targetFrameworkVersion = project.PrimaryPropertyGroup
				.Elements(project.XmlNamespace + "TargetFrameworkVersion")
				.FirstOrDefault();
			if (targetFrameworkVersion?.Value != null)
			{
				project.TargetFrameworks.Add(ToTargetFramework(targetFrameworkVersion.Value));
			}
			else
			{
				var targetFrameworks = project.PrimaryPropertyGroup
					.Elements(project.XmlNamespace + "TargetFrameworks")
					.FirstOrDefault()
					?.Value;
				if (targetFrameworks != null)
				{
					foreach (var framework in targetFrameworks.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
					)
					{
						project.TargetFrameworks.Add(framework);
					}
				}
				else
				{
					var targetFramework = project.PrimaryPropertyGroup
						.Elements(project.XmlNamespace + "TargetFramework")
						.FirstOrDefault()
						?.Value;
					if (targetFramework != null)
					{
						project.TargetFrameworks.Add(targetFramework);
					}
					else
					{
						_logger.LogError(
							"TargetFramework cannot be determined from project file. The scenario is not supported and highly bug-prone.");
					}
				}
			}

			// Ref.: https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
			if (project.PrimaryPropertyGroup.Elements(project.XmlNamespace + "TestProjectType").Any() ||
			    project.PrimaryPropertyGroup.Elements(project.XmlNamespace + "ProjectTypeGuids").Any(e =>
				    e.Value.IndexOf("3AC096D0-A1C2-E12C-1390-A8335801FDAB", StringComparison.OrdinalIgnoreCase) > -1))
			{
				project.Type = ApplicationType.TestProject;
			}
			else
			{
				project.Type = ToApplicationType(
					project.PrimaryPropertyGroup
						.Elements(project.XmlNamespace + "OutputType")
						.FirstOrDefault()
						?.Value
					?? project.AdditionalPropertyGroups
						.Elements(project.XmlNamespace + "OutputType")
						.FirstOrDefault()
						?.Value
					?? (project.IsModernProject ? "library" : null));

				if (project.Type == ApplicationType.Unknown)
				{
					throw new NotSupportedException("Unable to parse output type.");
				}
			}

			(project.Configurations, project.Platforms) = ReadConfigurationPlatformVariants(project);

			project.BuildEvents = new[] {project.PrimaryPropertyGroup}.Concat(project.AdditionalPropertyGroups)
				.Elements()
				.Where(x =>
					x.Name.LocalName == "PostBuildEvent" ||
					x.Name.LocalName == "PreBuildEvent")
				.ToArray();

			project.Imports = project.ProjectDocument.Root
				.Elements(project.XmlNamespace + "Import")
				.Where(x => !RemoveMSBuildImports.Contains(x.Attribute("Project")?.Value))
				.ToArray();

			project.Targets = project.ProjectDocument.Root
				.Elements(project.XmlNamespace + "Target")
				.ToArray();
		}

		private static (List<string>, List<string>) ReadConfigurationPlatformVariants(Project project)
		{
			var configurationSet = new HashSet<string>();
			var platformSet = new HashSet<string>();

			var configurationsFromProperty = ParseFromProperty("Configurations");
			var platformsFromProperty = ParseFromProperty("Platforms");

			if (configurationsFromProperty != null)
			{
				foreach (var configuration in configurationsFromProperty)
				{
					configurationSet.Add(configuration);
				}
			}
			else
			{
				configurationSet.Add("Debug");
				configurationSet.Add("Release");
			}

			if (platformsFromProperty != null)
			{
				foreach (var platform in platformsFromProperty)
				{
					platformSet.Add(platform);
				}
			}
			else
			{
				platformSet.Add("AnyCPU");
			}

			var configurationList = configurationSet.ToList();
			var platformList = platformSet.ToList();
			configurationList.Sort();
			platformList.Sort();
			return (configurationList, platformList);

			string[] ParseFromProperty(string name) => project.PrimaryPropertyGroup
				.Elements(project.XmlNamespace + name)
				.FirstOrDefault()
				?.Value
				.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
		}

		private static void ReadPropertyGroups(Project project)
		{
			var (conditional, unconditional) = project.ProjectDocument.Root
				.Elements(project.XmlNamespace + "PropertyGroup")
				.Split(x => x.Attribute("Condition") != null);

			if (unconditional.Count == 0)
			{
				throw new NotSupportedException(
					"No unconditional property group found. Cannot determine important properties like target framework and others.");
			}

			project.AdditionalPropertyGroups = conditional;

			project.PrimaryPropertyGroup = unconditional[0];

			foreach (var child in unconditional.Skip(1).SelectMany(x => x.Elements()))
			{
				project.PrimaryPropertyGroup.Add(child);
			}
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