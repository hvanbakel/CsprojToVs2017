using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class ImportsTargetsFilterPackageReferencesTransformation : ILegacyOnlyProjectTransformation
	{
		public void Transform(Project definition)
		{
			if (definition.PackageReferences == null || definition.PackageReferences.Count == 0)
			{
				return;
			}

			var projectPath = definition.ProjectFolder.FullName;

			var nugetRepositoryPath = definition.NuGetPackagesPath.FullName;

			var packageReferenceIds = definition.PackageReferences.Select(x => x.Id).ToArray();

			var adjustedPath = Extensions.MaybeAdjustFilePath(nugetRepositoryPath);
			var packagePaths = packageReferenceIds.Select(packageId => Path.Combine(adjustedPath, packageId).ToLower())
												  .ToArray();

			definition.Imports = FilteredImports(definition.Imports, packagePaths, projectPath);
			definition.Targets = FilteredTargets(definition.Targets, packagePaths, projectPath);
		}

		private static List<XElement> FilteredImports(IReadOnlyList<XElement> imports, string[] packagePaths, string projectPath)
		{
			var filteredImports = imports
									.Where(import => !packagePaths.Any(
											packagePath => ImportMatchesPackage(import, packagePath)
										)
									).ToList();

			return filteredImports;

			bool ImportMatchesPackage(XElement import, string packagePath)
			{
				var importedProject = import.Attribute("Project")?.Value;
				if (importedProject == null)
				{
					return false;
				}

				var fullImportPath = Path.IsPathRooted(importedProject)
					? importedProject
					: Path.GetFullPath(Path.Combine(projectPath, importedProject));

				return fullImportPath.ToLower().StartsWith(packagePath);
			}
		}

		private static List<XElement> FilteredTargets(IReadOnlyList<XElement> targets, string[] packagePaths, string projectPath)
		{
			var filteredImports = targets
									.Where(import => !packagePaths.Any(
											packagePath => TargetMatchesPackage(import, packagePath)
										)
									).ToList();

			return filteredImports;

			bool TargetMatchesPackage(XElement target, string packagePath)
			{
				//To make sure we don't remove anything customly added, look for a
				//very specific vanilla target as created by nuget

				var propertyGroups = target.ElementsAnyNamespace("PropertyGroup")
										   .ToList();

				if (propertyGroups.Count() > 1)
				{
					//Expect no more than 1 property group
					return false;
				}

				var errorTextPropertyGroup = propertyGroups.SingleOrDefault();

				var properties = errorTextPropertyGroup?
									.Elements()
									.ToList();

				if ((properties?.Count() ?? 0) > 1)
				{
					//Expect no more than 1 'ErrorText' element
					return false;
				}

				var errorTextElement = properties?.SingleOrDefault();

				if (errorTextElement != null && errorTextElement.Name.LocalName != "ErrorText")
				{
					//Some other property
					return false;
				}

				var otherElements = target
									 .Elements()
									 .Where(x => x.Name.LocalName != "PropertyGroup").ToList();

				if (otherElements.Count() > 1)
				{
					return false;
				}

				var errorElement = otherElements.SingleOrDefault(x => x.Name.LocalName == "Error");

				var errorCondition = errorElement?.Attribute("Condition");

				if (errorCondition == null)
				{
					//Error element with condition is required
					return false;
				}

				var conditionPath = errorCondition.Value.Replace("!Exists('", "").Replace("')", "");

				var fullConditionPath = Path.IsPathRooted(conditionPath)
					? conditionPath
					: Path.GetFullPath(Path.Combine(projectPath, conditionPath));

				return fullConditionPath.ToLower().StartsWith(packagePath);
			}
		}
	}
}
