using System;
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
			var filteredImports = new List<XElement>();
			foreach (var target in targets)
			{
				var propertyGroups = target.ElementsAnyNamespace("PropertyGroup")
					.ToList();

				// Expect no more than 1 property group
				if (propertyGroups.Count == 1)
				{
					var errorTextPropertyGroup = propertyGroups.SingleOrDefault();

					var properties = errorTextPropertyGroup?
						.Elements()
						.ToList();

					// Expect no more than 1 'ErrorText' element
					if ((properties?.Count ?? 0) == 1)
					{
						var errorTextElement = properties?.SingleOrDefault();

						// Some other property
						if (errorTextElement == null || errorTextElement.Name.LocalName == "ErrorText")
						{
							var otherElements = target
								.Elements()
								.Where(x => x.Name.LocalName != "PropertyGroup")
								.ToList();

							if (otherElements.All(x => x.Name.LocalName == "Error"))
							{
								var matched = true;
								foreach (var errorElement in otherElements)
								{
									var errorCondition = errorElement.Attribute("Condition");

									// Error element with condition is required
									if (errorCondition == null)
									{
										// this target is not what we need
										matched = false;
										break;
									}
									var conditionPath = errorCondition.Value.Replace("!Exists('", "")
										.Replace("')", "");

									var fullConditionPath = Path.IsPathRooted(conditionPath)
										? conditionPath
										: Path.GetFullPath(Path.Combine(projectPath, conditionPath));

									if (packagePaths.All(packagePath =>
										!fullConditionPath.StartsWith(packagePath,
											StringComparison.CurrentCultureIgnoreCase)))
									{
										matched = false;
										break;
									}
								}

								if (matched)
								{
									// Continue iterating targets, filter this one OUT.
									continue;
								}
							}
						}
					}
				}

				filteredImports.Add(target);
			}

			return filteredImports;
		}
	}
}
