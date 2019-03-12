using System.IO;
using NuGet.ProjectModel;

namespace Project2015To2017.CleanUp
{
	public static class LockFileService
	{
		public static LockFile GetLockFile(string projectPath, string outputPath)
		{
			// Run the restore command
			var dotNetRunner = new DotNetRunner();
			var arguments = new[] { "restore", $"\"{projectPath}\"" };

			dotNetRunner.Run(Path.GetDirectoryName(projectPath), arguments);

			// Load the lock file
			var lockFilePath = Path.Combine(outputPath, "project.assets.json");
			return LockFileUtilities.GetLockFile(lockFilePath, NuGet.Common.NullLogger.Instance);
		}
	}
}