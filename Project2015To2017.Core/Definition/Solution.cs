using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Configuration;

namespace Project2015To2017.Definition
{
	public sealed class Solution
	{
		public FileInfo FilePath { get; set; }
		public IReadOnlyList<ProjectReference> ProjectPaths { get; set; }
		public DirectoryInfo SolutionFolder => this.FilePath.Directory;
		public IReadOnlyList<string> UnsupportedProjectPaths { get; set; }

		/// <summary>
		/// The directory where nuget stores its extracted packages for the solution.
		/// In general this is the 'packages' folder within the solution oflder, but
		/// it can be overridden, which is accounted for here.
		/// </summary>
		public DirectoryInfo NuGetPackagesPath
		{
			get
			{
				var solutionFolder = this.SolutionFolder.FullName;

				var nuGetSettings = Settings.LoadDefaultSettings(solutionFolder);
				var repositoryPathSetting = SettingsUtility.GetRepositoryPath(nuGetSettings);

				//return the explicitly set path, or if there isn't one, then assume the 'packages' folder is in the solution folder
				var path = repositoryPathSetting ?? Path.GetFullPath(Path.Combine(solutionFolder, "packages"));

				return new DirectoryInfo(Extensions.MaybeAdjustFilePath(path, solutionFolder));
			}
		}

		private sealed class FilePathEqualityComparer : IEqualityComparer<Solution>
		{
			public bool Equals(Solution x, Solution y)
			{
				if (ReferenceEquals(x, y)) return true;
				if (x is null) return false;
				if (y is null) return false;
				if (x.GetType() != y.GetType()) return false;
				return string.Equals(x.FilePath.FullName, y.FilePath.FullName, StringComparison.InvariantCultureIgnoreCase);
			}

			public int GetHashCode(Solution obj)
			{
				return (obj.FilePath != null ? StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.FilePath.FullName) : 0);
			}
		}

		public static IEqualityComparer<Solution> FilePathComparer { get; } = new FilePathEqualityComparer();
	}
}
