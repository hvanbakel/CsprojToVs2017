using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Project2015To2017.Definition;
using static Project2015To2017.Definition.Project;

namespace Project2015To2017.Reading
{
	public static class ProjectPropertiesReader
	{
		public static void PopulateProperties(Project project, XDocument projectXml)
		{
			var propertyGroups = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "PropertyGroup")
				.ToArray();

			var unconditionalPropertyGroups = propertyGroups.Where(x => x.Attribute("Condition") == null).ToArray();
			if (unconditionalPropertyGroups.Length == 0)
			{
				throw new NotSupportedException(
					"No unconditional property group found. Cannot determine important properties like target framework and others.");
			}

			project.Optimize =
				"true".Equals(unconditionalPropertyGroups.Elements(XmlNamespace + "Optimize").FirstOrDefault()?.Value,
					StringComparison.OrdinalIgnoreCase);
			project.TreatWarningsAsErrors =
				"true".Equals(
					unconditionalPropertyGroups.Elements(XmlNamespace + "TreatWarningsAsErrors").FirstOrDefault()
						?.Value, StringComparison.OrdinalIgnoreCase);
			project.AllowUnsafeBlocks =
				"true".Equals(
					unconditionalPropertyGroups.Elements(XmlNamespace + "AllowUnsafeBlocks").FirstOrDefault()?.Value,
					StringComparison.OrdinalIgnoreCase);

			project.RootNamespace = unconditionalPropertyGroups.Elements(XmlNamespace + "RootNamespace")
				.FirstOrDefault()?.Value;
			project.AssemblyName = unconditionalPropertyGroups.Elements(XmlNamespace + "AssemblyName").FirstOrDefault()
				?.Value;

			project.SignAssembly =
				"true".Equals(
					unconditionalPropertyGroups.Elements(XmlNamespace + "SignAssembly").FirstOrDefault()?.Value,
					StringComparison.OrdinalIgnoreCase);
			if (bool.TryParse(
				unconditionalPropertyGroups.Elements(XmlNamespace + "DelaySign").FirstOrDefault()?.Value,
				out bool delaySign))
				project.DelaySign = delaySign;
			project.AssemblyOriginatorKeyFile = unconditionalPropertyGroups
				.Elements(XmlNamespace + "AssemblyOriginatorKeyFile").FirstOrDefault()?.Value;

			var targetFramework = unconditionalPropertyGroups.Elements(XmlNamespace + "TargetFrameworkVersion").FirstOrDefault();
			if (targetFramework?.Value != null)
			{
				project.TargetFrameworks.Add(ToTargetFramework(targetFramework.Value));
				targetFramework.Remove();
			}

			// Ref.: https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
			if (unconditionalPropertyGroups.Elements(XmlNamespace + "TestProjectType").Any() ||
			    unconditionalPropertyGroups.Elements(XmlNamespace + "ProjectTypeGuids").Any(e =>
				    e.Value.IndexOf("3AC096D0-A1C2-E12C-1390-A8335801FDAB", StringComparison.OrdinalIgnoreCase) > -1))
			{
				project.Type = ApplicationType.TestProject;
			}
			else
			{
				project.Type = ToApplicationType(
					unconditionalPropertyGroups.Elements(XmlNamespace + "OutputType").FirstOrDefault()?.Value ??
					propertyGroups.Elements(XmlNamespace + "OutputType").FirstOrDefault()?.Value);
			}

			(project.Configurations, project.Platforms) = ReadConditionals(unconditionalPropertyGroups, projectXml);

			project.BuildEvents = propertyGroups.Elements().Where(x =>
				x.Name == XmlNamespace + "PostBuildEvent" || x.Name == XmlNamespace + "PreBuildEvent").ToArray();
			project.AdditionalPropertyGroups = ReadAdditionalPropertyGroups(project, propertyGroups);

			project.Imports = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "Import").Where(x =>
				x.Attribute("Project")?.Value != @"$(MSBuildToolsPath)\Microsoft.CSharp.targets" &&
				x.Attribute("Project")?.Value != @"$(MSBuildBinPath)\Microsoft.CSharp.targets" &&
				x.Attribute("Project")?.Value !=
				@"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props").ToArray();

			project.Targets = projectXml.Element(XmlNamespace + "Project").Elements(XmlNamespace + "Target").ToArray();

			if (project.Type == ApplicationType.Unknown)
			{
				throw new NotSupportedException("Unable to parse output type.");
			}
		}

		private static (List<string>, List<string>) ReadConditionals(XElement[] unconditionalPropertyGroups,
			XDocument projectXml)
		{
			var configurationSet = new HashSet<string>();
			var platformSet = new HashSet<string>();

			var configurationsFromProperty = ParseFromProperty("Configurations");
			var platformsFromProperty = ParseFromProperty("Platforms");

			if (configurationsFromProperty != null)
			{
				foreach (var configuration in configurationsFromProperty)
					configurationSet.Add(configuration);
			}

			if (platformsFromProperty != null)
			{
				foreach (var platform in platformsFromProperty)
					platformSet.Add(platform);
			}

			var (needConfigurations, needPlatforms) = (configurationsFromProperty == null, platformsFromProperty == null);

			if (needConfigurations || needPlatforms)
			{
				foreach (var x in projectXml.Descendants())
				{
					var condition = x.Attribute("Condition");
					if (condition == null) continue;

					var conditionValue = condition.Value;
					if (!conditionValue.Contains("$(Configuration)") && !conditionValue.Contains("$(Platform)")) continue;

					var conditionEvaluated = ConditionEvaluator.GetConditionValues(conditionValue);

					if (needConfigurations && conditionEvaluated.TryGetValue("Configuration", out var configurations))
					{
						foreach (var configuration in configurations)
							configurationSet.Add(configuration);
					}

					if (needPlatforms && conditionEvaluated.TryGetValue("Platform", out var platforms))
					{
						foreach (var platform in platforms)
							platformSet.Add(platform);
					}
				}
			}

			var configurationList = configurationSet.ToList();
			var platformList = platformSet.ToList();
			configurationList.Sort();
			platformList.Sort();
			return (configurationList, platformList);

			string[] ParseFromProperty(string name) => unconditionalPropertyGroups.Elements(XmlNamespace + name)
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

			FilterUnneededProperties(project, additionalPropertyGroups);

			return additionalPropertyGroups;
		}

		private static void FilterUnneededProperties(Project project, IList<XElement> additionalPropertyGroups)
		{
			// special case handling for when condition-guarded props override global props not set to their defaults
			var globalOverrides = RetrieveGlobalOverrides(additionalPropertyGroups);

			if (!globalOverrides.TryGetValue("ProjectName", out var projectName))
				if (!globalOverrides.TryGetValue("AssemblyName", out projectName))
					projectName = project.FilePath.Name.Replace(".csproj", "");

			var removeQueue = new List<XElement>();

			foreach (var propertyGroup in additionalPropertyGroups)
			{
				foreach (var child in propertyGroup.Elements())
				{
					var parentCondition = propertyGroup.Attribute("Condition")?.Value.Trim() ?? "";
					var hasParentCondition = parentCondition.Length > 1; // no sane condition is 1 char long
					var parentConditionEvaluated = ConditionEvaluator.GetNonAmbiguousConditionContracts(parentCondition);
					var parentConditionHasPlatform =
						parentConditionEvaluated.TryGetValue("Platform", out var parentConditionPlatform);
					var parentConditionHasConfiguration =
						parentConditionEvaluated.TryGetValue("Configuration", out var parentConditionConfiguration);
					var parentConditionPlatformLower = parentConditionPlatform?.ToLowerInvariant();
					var parentConditionConfigurationLower = parentConditionConfiguration?.ToLowerInvariant();
					var isDebugOnly = parentConditionHasConfiguration && parentConditionConfigurationLower == "debug";
					var isReleaseOnly = parentConditionHasConfiguration && parentConditionConfigurationLower == "release";

					var tagLocalName = child.Name.LocalName;
					var valueLower = child.Value.ToLowerInvariant();
					var valueLowerTrim = valueLower.Trim();
					var hasGlobalOverride = globalOverrides.TryGetValue(tagLocalName, out var globalOverride);
					var globalOverrideLower = globalOverride?.ToLowerInvariant();

					// Regex is the easiest way to replace string between two unknown chars preserving both as is
					// so that bin\Debug\net472 is turned into bin\$(Configuration)\net472
					// and bin/Debug\net472 is turned into bin/$(Configuration)\net472, preserving path separators
					var configurationPathRegex = parentConditionHasConfiguration
						? new Regex($@"([\\/]){parentConditionConfiguration}([\\/])")
						: null;

					var childCondition = child.Attribute("Condition");
					switch (tagLocalName)
					{
						// VS2013 NuGet bugs workaround
						case "NuGetPackageImportStamp":
						// used by Project2015To2017 and not needed anymore
						case "OutputType" when !hasParentCondition:
						case "Platforms" when !hasParentCondition:
						case "Configurations" when !hasParentCondition:
						case "TargetFrameworkVersion" when !hasParentCondition:
						case "TargetFrameworkIdentifier" when !hasParentCondition:
						case "TargetFrameworkProfile" when !hasParentCondition && valueLowerTrim.Length == 0:
						// VCS properties
						case "SccProjectName" when !hasParentCondition && valueLowerTrim.Length == 0:
						case "SccLocalPath" when !hasParentCondition && valueLowerTrim.Length == 0:
						case "SccAuxPath" when !hasParentCondition && valueLowerTrim.Length == 0:
						case "SccProvider" when !hasParentCondition && valueLowerTrim.Length == 0:
						// Project properties set to defaults (Microsoft.NET.Sdk)
						case "OutputType" when ValidateDefaultValue("Library"):
						case "FileAlignment" when ValidateDefaultValue("512"):
						case "ErrorReport" when ValidateDefaultValue("prompt"):
						case "Deterministic" when ValidateDefaultValue("true"):
						case "WarningLevel" when ValidateDefaultValue("4"):
						case "DebugType" when ValidateDefaultValue("portable"):
						case "ResolveNuGetPackages" when ValidateDefaultValue("false"):
						case "SkipImportNuGetProps" when ValidateDefaultValue("true"):
						case "SkipImportNuGetBuildTargets" when ValidateDefaultValue("true"):
						case "RestoreProjectStyle" when ValidateDefaultValue("packagereference"):
						case "AllowUnsafeBlocks" when ValidateDefaultValue("false"):
						case "TreatWarningsAsErrors" when ValidateDefaultValue("false"):
						case "Prefer32Bit" when ValidateDefaultValue("false"):
						case "SignAssembly" when ValidateDefaultValue("false"):
						case "DelaySign" when ValidateDefaultValue("false"):
						case "GeneratePackageOnBuild" when ValidateDefaultValue("false"):
						case "PackageRequireLicenseAcceptance" when ValidateDefaultValue("false"):
						case "DebugSymbols" when ValidateDefaultValue("false"):
						case "CheckForOverflowUnderflow" when ValidateDefaultValue("false"):
						case "AppendTargetFrameworkToOutputPath" when ValidateDefaultValue("true"):
						case "AppDesignerFolder" when ValidateDefaultValue("properties"):
						case "DefaultProjectTypeGuid" when ValidateDefaultValue("{fae04ec0-301f-11d3-bf4b-00c04f79efbc}"):
						case "DefaultLanguageSourceExtension" when ValidateDefaultValue(".cs"):
						case "Language" when ValidateDefaultValue("C#"):
						case "TargetRuntime" when ValidateDefaultValue("managed"):
						case "Utf8Output" when ValidateDefaultValue("true"):
						case "PlatformName" when ValidateDefaultValue("$(platform)")
						                         || (parentConditionHasPlatform &&
						                             ValidateDefaultValue(parentConditionPlatformLower)):
						// Conditional platform default values
						case "PlatformTarget" when parentConditionHasPlatform && child.Value == parentConditionPlatform
						                                                      && !hasGlobalOverride:
						// Conditional configuration (Debug/Release) default values
						case "DefineConstants" when isDebugOnly && ValidateDefaultConstants(valueLower, "debug", "trace") &&
						                            !hasGlobalOverride:
						case "DefineConstants" when isReleaseOnly && ValidateDefaultConstants(valueLower, "trace") &&
						                            !hasGlobalOverride:
						case "Optimize" when isDebugOnly && valueLower == "false" && !hasGlobalOverride:
						case "Optimize" when isReleaseOnly && valueLower == "true" && !hasGlobalOverride:
						case "DebugSymbols" when isDebugOnly && valueLower == "true" && !hasGlobalOverride:
						// Default project values for Platform and Configuration
						case "Platform" when !hasParentCondition && ValidateEmptyConditionValue(childCondition) &&
						                     valueLower == "anycpu":
						case "Configuration" when !hasParentCondition && ValidateEmptyConditionValue(childCondition) &&
						                          valueLower == "debug":
						// Extra ProjectName duplicates
						case "RootNamespace" when !hasParentCondition && ValidateEmptyConditionValue(childCondition) &&
						                          child.Value == projectName:
						case "AssemblyName" when !hasParentCondition && ValidateEmptyConditionValue(childCondition) &&
						                         child.Value == projectName:
						{
							removeQueue.Add(child);
							break;
						}

						// Default configuration-specific paths
						// todo: move duplicated paths from conditional groups to non-conditional one
						case "OutputPath"
							when parentConditionHasConfiguration && configurationPathRegex.IsMatch(child.Value):
						{
							child.Value = configurationPathRegex.Replace(child.Value, "$1$(Configuration)$2");
							break;
						}
					}

					// following local methods will capture parent scope
					bool ValidateDefaultValue(string @default)
					{
						return (!hasParentCondition && valueLower == @default) ||
						       (hasParentCondition && ValidateConditionedDefaultValue(@default));
					}

					bool ValidateConditionedDefaultValue(string @default)
					{
						return (valueLower == @default) && (!hasGlobalOverride || globalOverrideLower == @default);
					}

					// following local methods will not capture parent scope
					bool ValidateEmptyConditionValue(XAttribute condition)
					{
						if (condition == null)
							return true;
						var value = condition.Value;
						return (value.Count(x => x == '=') == 2) && value.Contains("''");
					}

					bool ValidateDefaultConstants(string value, params string[] expected)
					{
						var defines = value.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
						var set = new HashSet<string>(defines);
						foreach (var expecto in expected)
							if (!set.Remove(expecto))
								return false;
						return set.Count == 0;
					}
				}
			}

			// we cannot remove elements correctly while iterating through elements, 2nd pass is needed
			foreach (var child in removeQueue)
				child.Remove();
		}

		/// <summary>
		/// Get all non-conditional properties and their respective values
		/// </summary>
		/// <param name="additionalPropertyGroups">PropertyGroups to be inspected</param>
		/// <returns>Dictionary of properties' keys and values</returns>
		private static IDictionary<string, string> RetrieveGlobalOverrides(IList<XElement> additionalPropertyGroups)
		{
			var globalOverrides = new Dictionary<string, string>();
			foreach (var propertyGroup in additionalPropertyGroups)
			{
				if (!HasEmptyCondition(propertyGroup))
					continue;

				foreach (var child in propertyGroup.Elements())
				{
					if (!HasEmptyCondition(child))
						continue;

					globalOverrides[child.Name.LocalName] = child.Value.Trim();
				}
			}

			return globalOverrides;

			bool HasEmptyCondition(XElement element)
			{
				var conditionAttribute = element.Attribute("Condition");
				if (conditionAttribute == null)
					return true;

				var condition = conditionAttribute.Value.Trim() ?? "";

				// no sane condition is 1 char long
				return condition.Length <= 1;
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