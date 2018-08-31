using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Reading
{
	public sealed class ProjectPropertiesReader
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

			var targetFrameworkVersion = project.Property("TargetFrameworkVersion")?.Value;
			if (targetFrameworkVersion != null)
			{
				project.TargetFrameworks.Add(ToTargetFramework(targetFrameworkVersion));
			}
			else
			{
				var targetFrameworks = project.Property("TargetFrameworks")?.Value;
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
					var targetFramework = project.Property("TargetFramework")?.Value;
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
			if (project.PropertyGroups.ElementsAnyNamespace("TestProjectType").Any() ||
			    project.Property("ProjectTypeGuids")
				    ?.Value
				    .IndexOf("3AC096D0-A1C2-E12C-1390-A8335801FDAB", StringComparison.OrdinalIgnoreCase) > -1)
			{
				project.Type = ApplicationType.TestProject;
			}
			else
			{
				project.Type = ToApplicationType(
					project.Property("OutputType", tryConditional: true)?.Value
					?? (project.IsModernProject ? "library" : null));

				if (project.Type == ApplicationType.Unknown)
				{
					throw new NotSupportedException("Unable to parse output type.");
				}
			}

			(project.Configurations, project.Platforms) = ReadConfigurationPlatformVariants(project);

			project.BuildEvents = project.PropertyGroups
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

			string[] ParseFromProperty(string name) => project.Property(name)
				?.Value
				.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
		}

		internal static void ReadPropertyGroups(Project project)
		{
			project.PropertyGroups = project.ProjectDocument.Root
				.Elements(project.XmlNamespace + "PropertyGroup")
				.ToList();

			try
			{
				var _ = project.PrimaryPropertyGroup();
			}
			catch (InvalidOperationException)
			{
				throw new NotSupportedException(
					"No unconditional property group found. Cannot determine important properties like target framework and others.");
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
				case "appcontainerexe": return ApplicationType.AppContainerExe;
				default: throw new NotSupportedException($"OutputType {outputType} is not supported.");
			}
		}
	}
}