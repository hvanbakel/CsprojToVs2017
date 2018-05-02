using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal sealed class RemovePackageImportsTransformation : ITransformation
	{
		public void Transform(Project definition, IProgress<string> progress)
		{
			if (definition.PackageReferences == null || definition.PackageReferences.Count == 0)
			{
				return;
			}

			var projectPath = definition.ProjectFolder.FullName;

			var nugetRepositoryPath = definition.NugetPackagesPath.FullName;

			var packageReferenceIds = definition.PackageReferences.Select(x => x.Id).ToArray();

			var packagePaths = packageReferenceIds.Select(packageId => Path.Combine(nugetRepositoryPath, packageId).ToLower())
												  .ToArray();

			var filteredAssemblies = definition.Imports
											   .Where(import => !packagePaths.Any(
														    packagePath => ImportMatchesPackage(import, packagePath)
													 )
											   )
											   .ToList();

			definition.Imports = filteredAssemblies;

			bool ImportMatchesPackage(XElement import, string packagePath)
			{
				var importedProject = import.Attribute("Project")?.Value;
				if (importedProject == null)
				{
					return false;
				}

				var fullImportPath = Path.IsPathRooted(importedProject) ? importedProject : Path.GetFullPath(Path.Combine(projectPath, importedProject));

				return fullImportPath.ToLower().StartsWith(packagePath);
			}
		}
	}
}
