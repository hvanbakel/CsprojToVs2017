using System;
using System.IO;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal sealed class RemovePackageAssemblyReferencesTransformation : ITransformation
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

			var filteredAssemblies = definition.AssemblyReferences
				.Where(assembly => !packagePaths.Any(
						packagePath => AssemblyMatchesPackage(assembly, packagePath)
					)
				)
				.ToList();

			definition.AssemblyReferences = filteredAssemblies;

			bool AssemblyMatchesPackage(AssemblyReference assembly, string packagePath)
			{
				var hintPath = assembly.HintPath;
				if (hintPath == null)
				{
					return false;
				}

				var fullHintPath = Path.IsPathRooted(hintPath) ? hintPath : Path.GetFullPath(Path.Combine(projectPath, hintPath));

				return fullHintPath.ToLower().StartsWith(packagePath);
			}
		}
	}
}
