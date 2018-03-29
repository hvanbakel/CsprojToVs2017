using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
	public class ProjectConverter
    {
        private static readonly IReadOnlyList<ITransformation> _transformationsToApply = new ITransformation[]
        {
            new ProjectPropertiesTransformation(),
            new ProjectReferenceTransformation(),
            new PackageReferenceTransformation(),
            new AssemblyReferenceTransformation(),
            new RemovePackageAssemblyReferencesTransformation(),
            new FileTransformation(),
            new AssemblyInfoTransformation(),
            new NugetPackageTransformation()
        };

        public static IEnumerable<Definition.Project> Convert(string target, IProgress<string> progress)
        {
			if (Path.GetExtension(target).Equals(".sln", StringComparison.OrdinalIgnoreCase))
			{
				progress.Report("Solution parsing started.");
				using (var reader = new StreamReader(File.OpenRead(target)))
				{
					var file = new FileInfo(target);
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						var projectPath = line.Split('"').FirstOrDefault(x => x.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));
						if (projectPath != null)
						{
							progress.Report("Project found: " + projectPath);
							var fullPath = Path.Combine(file.Directory.FullName, projectPath);
							if (!File.Exists(fullPath))
							{
								progress.Report("Project file not found at: " + fullPath);
							}
							else
							{
								yield return ProcessFile(fullPath, progress);
							}
						}
					}
				}

				yield break;
			}

            // Process all csprojs found in given directory
            if (!Path.GetExtension(target).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                var projectFiles = Directory.EnumerateFiles(target, "*.csproj", SearchOption.AllDirectories).ToArray();
                if (projectFiles.Length == 0)
                {
	                progress.Report($"Please specify a project file.");
					yield break;
                }
	            progress.Report($"Multiple project files found under directory {target}:");
	            progress.Report(string.Join(Environment.NewLine, projectFiles));
                foreach (var projectFile in projectFiles)
                {
                    yield return ProcessFile(projectFile, progress);
                }

				yield break;
            }

            // Process only the given project file
            yield return ProcessFile(target, progress);
			yield break;
        }

        private static Definition.Project ProcessFile(string filePath, IProgress<string> progress)
        {
            var file = new FileInfo(filePath);
            if (!Validate(file, progress))
			{
				return null;
			}

            XDocument xmlDocument;
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                xmlDocument = XDocument.Load(stream);
            }

            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
            if (xmlDocument.Element(nsSys + "Project") == null)
            {
	            progress.Report($"This is not a VS2015 project file.");
				return null;
			}

			var fileInfo = new FileInfo(filePath);
			var projectDefinition = new Project
			{
				FilePath = fileInfo
			};

            var directory = fileInfo.Directory;
            Task.WaitAll(_transformationsToApply.Select(
                    t => t.TransformAsync(xmlDocument, directory, projectDefinition, progress))
                .ToArray());

            AssemblyReferenceTransformation.RemoveExtraAssemblyReferences(projectDefinition);

            var projectFile = fileInfo.FullName;
            if (!SaveBackup(projectFile, progress))
            {
				return null;
			}

            var packagesFile = Path.Combine(fileInfo.DirectoryName, "packages.config");
            if (File.Exists(packagesFile))
            {
                if (!RenameFile(packagesFile, progress))
				{
					return null;
				}
            }

            var nuspecFile = fileInfo.FullName.Replace("csproj", "nuspec");
            if (File.Exists(nuspecFile))
            {
                if (!RenameFile(nuspecFile, progress))
                {
					return null;
                }
            }

			return projectDefinition;
        }

        internal static bool Validate(FileInfo file, IProgress<string> progress)
        {
            if (!file.Exists)
            {
	            progress.Report($"File {file.FullName} could not be found.");
                return false;
            }

            if (file.IsReadOnly)
            {
	            progress.Report($"File {file.FullName} is readonly, please make the file writable first (checkout from source control?).");
                return false;
            }

            return true;
        }

        private static bool SaveBackup(string filename, IProgress<string> progress)
        {
            var output = false;

            var backupFileName = filename + ".old";
            if (File.Exists(backupFileName))
            {
	            progress.Report($"Cannot create backup file. Please delete {backupFileName}.");
            }
            else
            {
                File.Copy(filename, filename + ".old");
                output = true;
            }

            return output;
        }

        private static bool RenameFile(string filename, IProgress<string> progress)
        {
            var output = false;

            var backupFileName = filename + ".old";
            if (File.Exists(backupFileName))
            {
	            progress.Report($"Cannot create backup file. Please delete {backupFileName}.");
            }
            else
            {
                // todo Consider using TF VC or Git?
                File.Move(filename, filename + ".old");
                output = true;
            }

            return output;
        }

    }
}
