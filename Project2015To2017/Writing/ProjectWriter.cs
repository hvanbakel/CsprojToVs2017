using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Writing
{
	public class ProjectWriter
	{
		private readonly Action<FileSystemInfo> deleteFileOperation;
		private readonly Action<FileSystemInfo> checkoutOperation;

		public ProjectWriter()
			: this(_ => { }, _ => { })
		{
		}

		public ProjectWriter(Action<FileSystemInfo> deleteFileOperation, Action<FileSystemInfo> checkoutOperation)
		{
			this.deleteFileOperation = deleteFileOperation;
			this.checkoutOperation = checkoutOperation;
		}

		public void Write(Project project, bool makeBackups, IProgress<string> progress)
		{
			if (makeBackups && !DoBackups(project, progress))
			{
				progress.Report("Couldn't do backup, so not applying any changes");
				return;
			}

			if (!WriteProjectFile(project, progress))
			{
				progress.Report("Aborting as could not write to project file");
				return;
			}

			if (!WriteAssemblyInfoFile(project, progress))
			{
				progress.Report("Aborting as could not write to assembly info file");
				return;
			}

			DeleteUnusedFiles(project, progress);
		}

		private bool WriteProjectFile(Project project, IProgress<string> progress)
		{
			var projectNode = CreateXml(project);

			var projectFile = project.FilePath;
			this.checkoutOperation(projectFile);

			if (projectFile.IsReadOnly)
			{
				progress.Report($"{projectFile} is readonly, please make the file writable first (checkout from source control?).");
				return false;
			}

			File.WriteAllText(projectFile.FullName, projectNode.ToString());
			return true;
		}

		private bool WriteAssemblyInfoFile(Project project, IProgress<string> progress)
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
				progress.Report($"{file} is readonly, please make the file writable first (checkout from source control?).");
				return false;
			}

			File.WriteAllText(file.FullName, newContents);

			return true;
		}

		private static bool DoBackups(Project project, IProgress<string> progress)
		{
			var projectFile = project.FilePath;

			var backupFolder = CreateBackupFolder(projectFile, progress);

			if (backupFolder == null)
			{
				return false;
			}

			progress.Report($"Backing up to {backupFolder.FullName}");

			projectFile.CopyTo(Path.Combine(backupFolder.FullName,  $"{projectFile.Name}.old"));

			var packagesFile = project.PackagesConfigFile;
			packagesFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{packagesFile.Name}.old"));

			var nuspecFile = project.PackageConfiguration?.NuspecFile;
			nuspecFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{nuspecFile.Name}.old"));

			var assemblyInfoFile = project.AssemblyAttributes?.File;
			assemblyInfoFile?.CopyTo(Path.Combine(backupFolder.FullName, $"{assemblyInfoFile.Name}.old"));

			return true;
		}

		private static DirectoryInfo CreateBackupFolder(FileInfo projectFile, IProgress<string> progress)
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

				var MaxIndex = 100;

				var foundBackupDir = Enumerable.Range(1, MaxIndex)
											   .Select(x => Path.Combine(baseDir, $"Backup{x}"))
											   .FirstOrDefault(x => !Directory.Exists(x));

				if (foundBackupDir == null)
				{
					progress.Report("Exhausted search for possible backup folder");
				}

				return foundBackupDir;
			}
		}

		internal XElement CreateXml(Project project)
		{
			var outputFile = project.FilePath;

			var projectNode = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"));

			projectNode.Add(GetMainPropertyGroup(project, outputFile));

			if (project.AdditionalPropertyGroups != null)
			{
				projectNode.Add(project.AdditionalPropertyGroups.Select(RemoveAllNamespaces));
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

			if (project.BuildEvents != null && project.BuildEvents.Any())
			{
				var propertyGroup = new XElement("PropertyGroup");
				projectNode.Add(propertyGroup);
				foreach (var buildEvent in project.BuildEvents.Select(RemoveAllNamespaces))
				{
					propertyGroup.Add(buildEvent);
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

					itemGroup.Add(projectReferenceElement);
				}

				projectNode.Add(itemGroup);
			}

			if (project.PackageReferences?.Count > 0)
			{
				var nugetReferences = new XElement("ItemGroup");
				foreach (var packageReference in project.PackageReferences)
				{
					var reference = new XElement("PackageReference", new XAttribute("Include", packageReference.Id), new XAttribute("Version", packageReference.Version));
					if (packageReference.IsDevelopmentDependency)
					{
						reference.Add(new XElement("PrivateAssets", "all"));
					}

					nugetReferences.Add(reference);
				}

				projectNode.Add(nugetReferences);
			}

			if (project.AssemblyReferences?.Count > 0)
			{
				var assemblyReferences = new XElement("ItemGroup");
				foreach (var assemblyReference in project.AssemblyReferences.Where(x => !IsDefaultIncludedAssemblyReference(x.Include)))
				{
					assemblyReferences.Add(MakeAssemblyReference(assemblyReference));
				}

				if (assemblyReferences.HasElements)
				{
					projectNode.Add(assemblyReferences);
				}
			}

			// manual includes
			if (project.IncludeItems?.Count > 0)
			{
				var includeGroup = new XElement("ItemGroup");
				foreach (var include in project.IncludeItems.Select(RemoveAllNamespaces))
				{
					includeGroup.Add(include);
				}

				projectNode.Add(includeGroup);
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

		private static XElement RemoveAllNamespaces(XElement e)
		{
			return new XElement(e.Name.LocalName,
			  (from n in e.Nodes()
			   select ((n is XElement) ? RemoveAllNamespaces((XElement)n) : n)),
				  (e.HasAttributes) ?
					(from a in e.Attributes()
					 where (!a.IsNamespaceDeclaration)
					 select new XAttribute(a.Name.LocalName, a.Value)) : null);
		}

		private bool IsDefaultIncludedAssemblyReference(string assemblyReference)
		{
			return new[]
			{
				"System",
				"System.Core",
				"System.Data",
				"System.Drawing",
				"System.IO.Compression.FileSystem",
				"System.Numerics",
				"System.Runtime.Serialization",
				"System.Xml",
				"System.Xml.Linq"
			}.Contains(assemblyReference);
		}

		private XElement GetMainPropertyGroup(Project project, FileInfo outputFile)
		{
			var mainPropertyGroup = new XElement("PropertyGroup");

			AddTargetFrameworks(mainPropertyGroup, project.TargetFrameworks);

			AddIfNotNull(mainPropertyGroup, "Configurations", string.Join(";", project.Configurations?.Distinct() ?? Array.Empty<string>()));
			AddIfNotNull(mainPropertyGroup, "Optimize", project.Optimize ? "true" : null);
			AddIfNotNull(mainPropertyGroup, "TreatWarningsAsErrors", project.TreatWarningsAsErrors ? "true" : null);
			AddIfNotNull(mainPropertyGroup, "RootNamespace", project.RootNamespace != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.RootNamespace : null);
			AddIfNotNull(mainPropertyGroup, "AssemblyName", project.AssemblyName != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.AssemblyName : null);
			AddIfNotNull(mainPropertyGroup, "AllowUnsafeBlocks", project.AllowUnsafeBlocks ? "true" : null);
			AddIfNotNull(mainPropertyGroup, "SignAssembly", project.SignAssembly ? "true" : null);
			AddIfNotNull(mainPropertyGroup, "DelaySign", project.DelaySign.HasValue ? (project.DelaySign.Value ? "true" : "false") : null);
			AddIfNotNull(mainPropertyGroup, "AssemblyOriginatorKeyFile", project.AssemblyOriginatorKeyFile);
			AddIfNotNull(mainPropertyGroup, "AppendTargetFrameworkToOutputPath", project.AppendTargetFrameworkToOutputPath ? null : "false");

			switch (project.Type)
			{
				case ApplicationType.ConsoleApplication:
					mainPropertyGroup.Add(new XElement("OutputType", "Exe"));
					break;
				case ApplicationType.WindowsApplication:
					mainPropertyGroup.Add(new XElement("OutputType", "WinExe"));
					break;
			}

			mainPropertyGroup.Add(project.AssemblyAttributeProperties);

			AddPackageNodes(mainPropertyGroup, project.PackageConfiguration);

			return mainPropertyGroup;
		}

		private void AddPackageNodes(XElement mainPropertyGroup, PackageConfiguration packageConfiguration)
		{
			if (packageConfiguration == null)
			{
				return;
			}

			//Add those properties not already covered by the project properties

			AddIfNotNull(mainPropertyGroup, "Authors", packageConfiguration.Authors);
			AddIfNotNull(mainPropertyGroup, "PackageIconUrl", packageConfiguration.IconUrl);
			AddIfNotNull(mainPropertyGroup, "PackageId", packageConfiguration.Id);
			AddIfNotNull(mainPropertyGroup, "PackageLicenseUrl", packageConfiguration.LicenseUrl);
			AddIfNotNull(mainPropertyGroup, "PackageProjectUrl", packageConfiguration.ProjectUrl);
			AddIfNotNull(mainPropertyGroup, "PackageReleaseNotes", packageConfiguration.ReleaseNotes);
			AddIfNotNull(mainPropertyGroup, "PackageTags", packageConfiguration.Tags);

			if (packageConfiguration.Id != null && packageConfiguration.Tags == null)
				mainPropertyGroup.Add(new XElement("PackageTags", "Library"));

			if (packageConfiguration.RequiresLicenseAcceptance)
			{
				mainPropertyGroup.Add(new XElement("PackageRequireLicenseAcceptance", "true"));
			}
		}

		private void AddIfNotNull(XElement node, string elementName, string value)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				node.Add(new XElement(elementName, value));
			}
		}

		private void AddTargetFrameworks(XElement mainPropertyGroup, IReadOnlyList<string> targetFrameworks)
		{
			if (targetFrameworks == null)
			{
				return;
			}
			else if (targetFrameworks.Count > 1)
			{
				AddIfNotNull(mainPropertyGroup, "TargetFrameworks", string.Join(";", targetFrameworks));
			}
			else
			{
				AddIfNotNull(mainPropertyGroup, "TargetFramework", targetFrameworks[0]);
			}
		}

		private void DeleteUnusedFiles(Project project, IProgress<string> progress)
		{
			var filesToDelete = new[]
			{
				project.PackageConfiguration?.NuspecFile,
				project.PackagesConfigFile
			}.Where(x => x != null)
			.Concat(project.Deletions);

			foreach (var fileInfo in filesToDelete)
			{
				if(fileInfo is DirectoryInfo directory && directory.EnumerateFileSystemInfos().Any())
				{
					progress.Report($"Directory {fileInfo.FullName} is not empty so will not delete");
					continue;
				}

				this.deleteFileOperation(fileInfo);
			}
		}

	}
}
