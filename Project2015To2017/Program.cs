using Project2015To2017.Definition;
using Project2015To2017.Writing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml.Linq;

[assembly: InternalsVisibleTo("Project2015To2017Tests")]

namespace Project2015To2017
{
    class Program
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

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine($"Please specify a project file.");
                return;
            }

            // Process all csprojs found in given directory
            if (Path.GetExtension(args[0]) != ".csproj")
            {
                var projectFiles = Directory.EnumerateFiles(args[0], "*.csproj", SearchOption.AllDirectories).ToArray();
                if (projectFiles.Length == 0)
                {
                    Console.WriteLine($"Please specify a project file.");
                    return;
                }
                Console.WriteLine($"Multiple project files found under directory {args[0]}:");
                Console.WriteLine(string.Join(Environment.NewLine, projectFiles));
                foreach (var projectFile in projectFiles)
                {
                    ProcessFile(projectFile);
                }

                return;
            }

            // Process only the given project file
            ProcessFile(args[0]);
        }

        private static void ProcessFile(string filePath)
        {
            var file = new FileInfo(filePath);
            if (!Validate(file))
            {
                return;
            }

            XDocument xmlDocument;
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                xmlDocument = XDocument.Load(stream);
            }

            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
            if (xmlDocument.Element(nsSys + "Project") == null)
            {
                Console.WriteLine($"This is not a VS2015 project file.");
                return;
            }

            var projectDefinition = new Project();

            var fileInfo = new FileInfo(filePath);
            var directory = fileInfo.Directory;
            Task.WaitAll(_transformationsToApply.Select(
                    t => t.TransformAsync(xmlDocument, directory, projectDefinition))
                .ToArray());

            AssemblyReferenceTransformation.RemoveExtraAssemblyReferences(projectDefinition);

            var projectFile = fileInfo.FullName;
            if (!SaveBackup(projectFile))
            {
                return;
            }

            var packagesFile = Path.Combine(fileInfo.DirectoryName, "packages.config");
            if (File.Exists(packagesFile))
            {
                if (!RenameFile(packagesFile))
                {
                    return;
                }
            }

            var nuspecFile = fileInfo.FullName.Replace("csproj", "nuspec");
            if (File.Exists(nuspecFile))
            {
                if (!RenameFile(nuspecFile))
                {
                    return;
                }
            }

            new ProjectWriter().Write(projectDefinition, fileInfo);
        }

        internal static bool Validate(FileInfo file)
        {
            if (!file.Exists)
            {
                Console.WriteLine($"File {file.FullName} could not be found.");
                return false;
            }

            if (file.IsReadOnly)
            {
                Console.WriteLine($"File {file.FullName} is readonly, please make the file writable first (checkout from source control?).");
                return false;
            }

            return true;
        }

        private static bool SaveBackup(string filename)
        {
            var output = false;

            var backupFileName = filename + ".old";
            if (File.Exists(backupFileName))
            {
                Console.Write($"Cannot create backup file. Please delete {backupFileName}.");
            }
            else
            {
                File.Copy(filename, filename + ".old");
                output = true;
            }

            return output;
        }

        private static bool RenameFile(string filename)
        {
            var output = false;

            var backupFileName = filename + ".old";
            if (File.Exists(backupFileName))
            {
                Console.Write($"Cannot create backup file. Please delete {backupFileName}.");
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
