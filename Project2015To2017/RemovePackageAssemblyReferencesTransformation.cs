using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using hvanbakel.Project2015To2017.Definition;
using NuGet.Configuration;

namespace hvanbakel.Project2015To2017
{
    internal sealed class RemovePackageAssemblyReferencesTransformation : ITransformation
	{
		public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition, IProgress<string> progress)
		{
			if (definition.PackageReferences == null || definition.PackageReferences.Count == 0)
			{
				return Task.CompletedTask;
			}

			var projectPath = projectFolder.FullName;

			var nugetRepositoryPath = NuGetRepositoryPath(projectPath);

			var packageReferenceIds = definition.PackageReferences.Select(x => x.Id).ToArray();

			var packagePaths = packageReferenceIds.Select(packageId => Path.Combine(nugetRepositoryPath, packageId).ToLower())
												  .ToArray();

			definition.AssemblyReferences.RemoveAll(assembly => 
				assembly.HintPath != null && 
				packagePaths.Any(packagePath => AssemblyMatchesPackage(assembly, packagePath)));

			return Task.CompletedTask;

			bool AssemblyMatchesPackage(AssemblyReference assembly, string packagePath)
			{
				var hintPath = assembly.HintPath;
				var fullHintPath = Path.IsPathRooted(hintPath) ? hintPath : Path.GetFullPath(Path.Combine(projectPath, hintPath));

				return fullHintPath.ToLower().StartsWith(packagePath);
			}
		}

		private static string NuGetRepositoryPath(string projectFolder)
		{
			var nuGetSettings = Settings.LoadDefaultSettings(projectFolder);
			var repositoryPathSetting = SettingsUtility.GetRepositoryPath(nuGetSettings);

			//return the explicitly set path, or if there isn't one, then assume the solution is one level
			//above the project and therefore so is the 'packages' folder
			return repositoryPathSetting ?? Path.GetFullPath(Path.Combine(projectFolder, @"..\packages"));
		}
	}
}
