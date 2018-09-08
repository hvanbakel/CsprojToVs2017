using System.IO;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class AssemblyFilterHintedPackageReferencesTransformation : ILegacyOnlyProjectTransformation
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

			var packagePaths = packageReferenceIds.Select(packageId => Path.Combine(nugetRepositoryPath, packageId).ToLower())
				.ToArray();

			var (filteredAssemblies, removeQueue) = definition.AssemblyReferences
				.Split(assembly => !packagePaths.Any(
						packagePath => AssemblyMatchesPackage(assembly, packagePath)
					)
				);

			foreach (var assemblyReference in removeQueue)
			{
				assemblyReference.DefinitionElement?.Remove();
			}

			definition.AssemblyReferences = filteredAssemblies;

			bool AssemblyMatchesPackage(AssemblyReference assembly, string packagePath)
			{
				var hintPath = assembly.HintPath;
				if (hintPath == null)
				{
					return false;
				}

				var fullHintPath = Path.IsPathRooted(hintPath) ? hintPath : Path.GetFullPath(Path.Combine(projectPath, hintPath));

				return Extensions.MaybeAdjustFilePath(fullHintPath).ToLower().StartsWith(Extensions.MaybeAdjustFilePath(packagePath));
			}
		}
	}
}
