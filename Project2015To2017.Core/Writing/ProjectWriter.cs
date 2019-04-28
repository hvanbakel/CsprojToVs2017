using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using static Project2015To2017.Extensions;

namespace Project2015To2017.Writing
{
	public sealed class ProjectWriter
	{
		private readonly ILogger logger;
		private readonly Action<FileSystemInfo> deleteFileOperation;
		private readonly Action<FileSystemInfo> checkoutOperation;

		public ProjectWriter(ILogger logger = null)
			: this(logger, _ => { }, _ => { })
		{
		}

		public ProjectWriter(Action<FileSystemInfo> deleteFileOperation, Action<FileSystemInfo> checkoutOperation)
			: this(null, deleteFileOperation, checkoutOperation)
		{

		}

		public ProjectWriter(ILogger logger, Action<FileSystemInfo> deleteFileOperation, Action<FileSystemInfo> checkoutOperation)
		{
			this.logger = logger ?? NoopLogger.Instance;
			this.deleteFileOperation = deleteFileOperation;
			this.checkoutOperation = checkoutOperation;
		}

		public bool TryWrite(Project project, bool makeBackups)
		{
			try
			{
				return TryWriteOrThrow(project, makeBackups);
			}
			catch (Exception e)
			{
				this.logger.LogError(e, "Project {Item} has thrown an exception during writing, skipping...", project.ProjectName);
				return false;
			}
		}

		public bool TryWriteOrThrow(Project project, bool makeBackups)
		{
			if (makeBackups && !DoBackups(project))
			{
				this.logger.LogError("Couldn't do backup, so not applying any changes");
				return false;
			}

			if (!WriteProjectFile(project))
			{
				this.logger.LogError("Aborting as could not write to project file");
				return false;
			}

			if (!WriteAssemblyInfoFile(project))
			{
				this.logger.LogError("Aborting as could not write to assembly info file");
				return false;
			}

			DeleteUnusedFiles(project);
			return true;
		}

		private bool WriteProjectFile(Project project)
		{
			var projectNode = CreateXml(project);

			var projectFile = project.FilePath;
			this.checkoutOperation(projectFile);

			if (projectFile.IsReadOnly)
			{
				this.logger.LogWarning($"{projectFile} is readonly, please make the file writable first (checkout from source control?).");
				return false;
			}

			File.WriteAllText(projectFile.FullName, projectNode.ToString(), Encoding.UTF8);
			return true;
		}

		private bool WriteAssemblyInfoFile(Project project)
		{
			var assemblyAttributes = project.AssemblyAttributes;

			if (assemblyAttributes?.File == null || project.Deletions.Any(x => assemblyAttributes.File.FullName == x.FullName))
			{
				return true;
			}

			var file = assemblyAttributes.File;
			var currentContents = File.ReadAllText(file.FullName);
			var newContents = assemblyAttributes.FileContents.ToFullString();

			if (newContents == currentContents)
			{
				//Nothing to do
				return true;
			}

			this.checkoutOperation(file);

			if (file.IsReadOnly)
			{
				this.logger.LogWarning($"{file} is readonly, please make the file writable first (checkout from source control?).");
				return false;
			}

			File.WriteAllText(file.FullName, newContents, Encoding.UTF8);

			return true;
		}

		private bool DoBackups(Project project)
		{
			var projectFile = project.FilePath;

			var backupFolder = CreateBackupFolder(projectFile);

			if (backupFolder == null)
			{
				return false;
			}

			this.logger.LogInformation($"Backing up to {backupFolder.FullName}");

			projectFile.CopyTo(Path.Combine(backupFolder.FullName, $"{projectFile.Name}.old"));

			var packagesFile = project.PackagesConfigFile;
			packagesFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{packagesFile.Name}.old"));

			var nuspecFile = project.PackageConfiguration?.NuspecFile;
			nuspecFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{nuspecFile.Name}.old"));

			var assemblyInfoFile = project.AssemblyAttributes?.File;
			assemblyInfoFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{assemblyInfoFile.Name}.old"));

			return true;
		}

		private DirectoryInfo CreateBackupFolder(FileInfo projectFile)
		{
			//Find a suitable backup directory that doesn't already exist
			var backupDir = ChooseBackupFolder();

			if (backupDir == null)
			{
				return null;
			}

			Directory.CreateDirectory(backupDir);

			return new DirectoryInfo(backupDir);

			string ChooseBackupFolder()
			{
				var baseDir = projectFile.DirectoryName;
				var trialDir = Path.Combine(baseDir, "Backup");

				if (!Directory.Exists(trialDir))
				{
					return trialDir;
				}

				const int maxIndex = 100;

				var foundBackupDir = Enumerable.Range(1, maxIndex)
					.Select(x => Path.Combine(baseDir, $"Backup{x}"))
					.FirstOrDefault(x => !Directory.Exists(x));

				if (foundBackupDir == null)
				{
					this.logger.LogWarning("Exhausted search for possible backup folder");
				}

				return foundBackupDir;
			}
		}

		internal XElement CreateXml(Project project)
		{
			var projectNode = new XElement("Project", new XAttribute("Sdk", project.ProjectSdk));

			if (project.PropertyGroups != null)
			{
				projectNode.Add(project.PropertyGroups.Select(RemoveAllNamespaces));
			}

			if (project.Imports != null)
			{
				foreach (var import in project.Imports.Select(RemoveAllNamespaces))
				{
					projectNode.Add(import);
				}
			}

			if (project.Targets != null)
			{
				foreach (var target in project.Targets.Select(RemoveAllNamespaces))
				{
					projectNode.Add(target);
				}
			}

			if (project.ProjectReferences?.Count > 0)
			{
				var itemGroup = new XElement("ItemGroup");
				foreach (var projectReference in project.ProjectReferences)
				{
					var projectReferenceElement = new XElement("ProjectReference",
						new XAttribute("Include", projectReference.Include));

					if (!string.IsNullOrWhiteSpace(projectReference.Aliases) && projectReference.Aliases != "global")
					{
						projectReferenceElement.Add(new XElement("Aliases", projectReference.Aliases));
					}

					if (projectReference.EmbedInteropTypes)
					{
						projectReferenceElement.Add(new XElement("EmbedInteropTypes", "true"));
					}

					if (projectReference.DefinitionElement != null)
					{
						projectReference.DefinitionElement.ReplaceWith(projectReferenceElement);
					}
					else
					{
						itemGroup.Add(projectReferenceElement);
					}
				}

				if (itemGroup.HasElements)
				{
					projectNode.Add(itemGroup);
				}
			}

			if (project.PackageReferences?.Count > 0)
			{
				var nugetReferences = new XElement("ItemGroup");
				foreach (var packageReference in project.PackageReferences)
				{
					var reference = new XElement("PackageReference", new XAttribute("Include", packageReference.Id));
					if (packageReference.Version != null)
					{
						reference.Add(new XAttribute("Version", packageReference.Version));
					}
					
					if (packageReference.IsDevelopmentDependency)
					{
						reference.Add(new XElement("PrivateAssets", "all"));
					}

					if (packageReference.DefinitionElement != null)
					{
						packageReference.DefinitionElement.ReplaceWith(reference);
					}
					else
					{
						nugetReferences.Add(reference);
					}
				}

				if (nugetReferences.HasElements)
				{
					projectNode.Add(nugetReferences);
				}
			}

			if (project.AssemblyReferences?.Count > 0)
			{
				var assemblyReferences = new XElement("ItemGroup");
				foreach (var assemblyReference in project.AssemblyReferences)
				{
					var assemblyReferenceElement = MakeAssemblyReference(assemblyReference);

					if (assemblyReference.DefinitionElement != null)
					{
						assemblyReference.DefinitionElement.ReplaceWith(assemblyReferenceElement);
					}
					else
					{
						assemblyReferences.Add(assemblyReferenceElement);
					}
				}

				if (assemblyReferences.HasElements)
				{
					projectNode.Add(assemblyReferences);
				}
			}

			// manual includes
			if (project.ItemGroups?.Count > 0)
			{
				foreach (var includeGroup in project.ItemGroups.Select(RemoveAllNamespaces))
				{
					projectNode.Add(includeGroup);
				}
			}

			return projectNode;
		}

		private static XElement MakeAssemblyReference(AssemblyReference assemblyReference)
		{
			var output = new XElement("Reference", new XAttribute("Include", assemblyReference.Include));

			if (assemblyReference.HintPath != null)
			{
				output.Add(new XElement("HintPath", assemblyReference.HintPath));
			}

			if (assemblyReference.Private != null)
			{
				output.Add(new XElement("Private", assemblyReference.Private));
			}

			if (assemblyReference.SpecificVersion != null)
			{
				output.Add(new XElement("SpecificVersion", assemblyReference.SpecificVersion));
			}

			if (assemblyReference.EmbedInteropTypes != null)
			{
				output.Add(new XElement("EmbedInteropTypes", assemblyReference.EmbedInteropTypes));
			}

			return output;
		}

		private void DeleteUnusedFiles(Project project)
		{
			var filesToDelete = new[]
				{
					project.PackageConfiguration?.NuspecFile,
					project.PackagesConfigFile
				}.Where(x => x != null)
				.Concat(project.Deletions);

			foreach (var fileInfo in filesToDelete)
			{
				if (fileInfo is DirectoryInfo directory && directory.EnumerateFileSystemInfos().Any())
				{
					this.logger.LogWarning($"Directory {fileInfo.FullName} is not empty so will not delete");
					continue;
				}

				var attributes = File.GetAttributes(fileInfo.FullName);
				if ((attributes & FileAttributes.ReadOnly) != 0)
				{
					this.logger.LogWarning($"File {fileInfo.FullName} could not be deleted as it is not writable.");
				}
				else
				{
					this.deleteFileOperation(fileInfo);
				}
			}
		}
	}
}
