using System.Collections.Generic;
using System.IO;
using NuGet.Configuration;

namespace Project2015To2017.Definition
{
	public sealed class Solution
	{
		public FileInfo FilePath { get; set; }
		public IReadOnlyList<ProjectReference> ProjectPaths { get; set; }
		public DirectoryInfo SolutionFolder => FilePath.Directory;

		/// <summary>
		/// The directory where nuget stores its extracted packages for the solution.
		/// In general this is the 'packages' folder within the solution oflder, but
		/// it can be overridden, which is accounted for here.
		/// </summary>
		public DirectoryInfo NuGetPackagesPath
		{
			get
			{
				var solutionFolder = SolutionFolder.FullName;

				var nuGetSettings = Settings.LoadDefaultSettings(solutionFolder);
				var repositoryPathSetting = SettingsUtility.GetRepositoryPath(nuGetSettings);

				//return the explicitly set path, or if there isn't one, then assume the 'packages' folder is in the solution folder
				var path = repositoryPathSetting ?? Path.GetFullPath(Path.Combine(solutionFolder, "packages"));

				return new DirectoryInfo(path);
			}
		}
	}
}
