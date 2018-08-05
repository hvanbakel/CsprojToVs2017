using System;
using System.IO;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	internal static class ExtensionMethods
	{
		public static string GetRelativePathTo(this FileSystemInfo from, FileSystemInfo to)
		{
			string GetPath(FileSystemInfo fsi)
			{
				return (fsi is DirectoryInfo d) ? (d.FullName.TrimEnd('\\') + "\\") : fsi.FullName;
			}

			var fromPath = GetPath(from);
			var toPath = GetPath(to);

			var fromUri = new Uri(fromPath);
			var toUri = new Uri(toPath);

			var relativeUri = fromUri.MakeRelativeUri(toUri);
			var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

			return relativePath.Replace('/', Path.DirectorySeparatorChar);
		}

		public static DirectoryInfo TryFindBestRootDirectory(this Project project)
		{
			if (project == null) throw new ArgumentNullException(nameof(project));

			return project.Solution?.FilePath.Directory ?? project.FilePath.Directory;
		}
	}
}