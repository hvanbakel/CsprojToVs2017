using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public sealed class ProjectPropertiesReader
	{
		private static readonly string[] RemoveMSBuildImports =
		{
			@"$(MSBuildToolsPath)\Microsoft.CSharp.targets",
			@"$(MSBuildBinPath)\Microsoft.CSharp.targets",
			@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props",
		};
		private static readonly Guid TestProjectTypeGuid = Guid.ParseExact("3AC096D0-A1C2-E12C-1390-A8335801FDAB", "D");

		private readonly ILogger logger;
		private readonly UnknownTargetFrameworkCallback unknownTargetFrameworkCallback;

		public ProjectPropertiesReader(ILogger logger, UnknownTargetFrameworkCallback unknownTargetFrameworkCallback = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
			this.unknownTargetFrameworkCallback = unknownTargetFrameworkCallback;
		}

		public void Read(Project project)
		{
			Debug.Assert(project.ProjectDocument.Root != null, "project.ProjectDocument.Root != null");

			ReadPropertyGroups(project);

			var projectSdk = project.ProjectDocument.Root.Attribute("Sdk")?.Value;
			if (projectSdk != null)
			{
				project.ProjectSdk = projectSdk;
			}

			ParseTargetFrameworks(project);
			if (!project.Valid)
				return;

			// Ref.: https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
			if (project.PropertyGroups.ElementsAnyNamespace("TestProjectType").Any() ||
			    project.IterateProjectTypeGuids().Any(x => x.guid == TestProjectTypeGuid))
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

		private void ParseTargetFrameworks(Project project)
		{
			var callbackStore = new List<(IReadOnlyList<string> frameworks, XElement source, string condition)>();

			var targetFrameworkAll = project.PropertyAll("TargetFrameworkVersion");
			if (ParseFromProperty(ProcessTargetFrameworkVersion))
				return;

			targetFrameworkAll = project.PropertyAll("TargetFrameworks");
			if (ParseFromProperty(ProcessTargetFrameworks))
				return;

			targetFrameworkAll = project.PropertyAll("TargetFramework");
			if (ParseFromProperty(ProcessTargetFramework))
				return;

			if (unknownTargetFrameworkCallback == null)
			{
				logger.LogError(
					"TargetFramework cannot be determined from project file. The scenario is not supported and highly bug-prone.");
				return;
			}

			if (!unknownTargetFrameworkCallback(project, callbackStore))
			{
				project.Valid = false;
			}

			bool ParseFromProperty(Func<string, IReadOnlyList<string>> elementValueFunc)
			{
				var targetFramework = GetUnambiguousFrameworkValue(targetFrameworkAll.unconditional);
				if (targetFrameworkAll.conditional.Count == 0 && targetFramework != null)
				{
					foreach (var tfm in elementValueFunc(targetFramework))
					{
						project.TargetFrameworks.Add(tfm);
					}

					return true;
				}

				foreach (var (condition, element) in targetFrameworkAll.conditional)
				{
					callbackStore.Add((elementValueFunc(element.Value), element, condition.Trim()));
				}

				foreach (var element in targetFrameworkAll.unconditional)
				{
					callbackStore.Add((elementValueFunc(element.Value), element, null));
				}

				return false;
			}

			string GetUnambiguousFrameworkValue(IReadOnlyList<XElement> elements)
			{
				if (elements.Count == 0)
					return null;
				var last = elements.Last().Value;
				return last.Contains("$") ? null : last;
			}

			IReadOnlyList<string> ProcessTargetFrameworkVersion(string s) => new[] {ToTargetFramework(s)};

			IReadOnlyList<string> ProcessTargetFrameworks(string s) => s
				.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries)
				.Select(x => x.Trim())
				.ToImmutableArray();

			IReadOnlyList<string> ProcessTargetFramework(string s) => new[] {s.Trim()};
		}

		private static (List<string> configurations, List<string> platforms)
			ReadConfigurationPlatformVariants(Project project)
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