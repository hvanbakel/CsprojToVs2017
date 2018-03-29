using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017
{
	internal sealed class NugetPackageTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition, IProgress<string> progress)
        {
            var nuspecFiles = projectFolder
                .EnumerateFiles("*.nuspec", SearchOption.AllDirectories)
                .ToArray();

            if (nuspecFiles.Length == 0)
            {
                progress.Report("No nuspec found, skipping package configuration.");
            }
            else if (nuspecFiles.Length == 1)
            {
	            progress.Report($"Reading package info from nuspec {nuspecFiles[0].FullName}.");

                XDocument nuspec;
                using (var filestream = File.Open(nuspecFiles[0].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    nuspec = XDocument.Load(filestream);
                }

                var namespaces = new XNamespace[]
                {
                    "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd",
                    "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd",
                    "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
                };
                foreach (var ns in namespaces)
                {
                    ExtractPackageConfiguration(definition, nuspec, ns);
                }

                if (definition.PackageConfiguration == null)
                {
	                progress.Report("Error reading package info from nuspec.");
                }
            }
            else
            {
	            progress.Report($@"Could not read from nuspec, multiple nuspecs found: 
{string.Join(Environment.NewLine, nuspecFiles.Select(x => x.FullName))}.");
            }

            return Task.CompletedTask;
        }

        private void ExtractPackageConfiguration(Project definition, XDocument nuspec, XNamespace ns)
        {
            var metadata = nuspec?.Element(ns + "package")?.Element(ns + "metadata");

            if (metadata != null)
            {
                var id = metadata.Element(ns + "id")?.Value;
                if (id == "$id$")
                {
                    id = null;
                }

                var version = metadata.Element(ns + "version")?.Value;
                if (version == "$version$")
                {
                    version = null;
                }

                definition.PackageConfiguration = new PackageConfiguration
                {
                    Id = id,
                    Version = version,
                    Authors = ReadValueAndReplace(metadata, ns + "authors", definition.AssemblyAttributes),
                    Description = ReadValueAndReplace(metadata, ns + "description", definition.AssemblyAttributes),
                    Copyright = ReadValueAndReplace(metadata, ns + "copyright", definition.AssemblyAttributes),
                    LicenseUrl = ReadValueAndReplace(metadata, ns + "licenseUrl", definition.AssemblyAttributes),
                    ProjectUrl = ReadValueAndReplace(metadata, ns + "projectUrl", definition.AssemblyAttributes),
                    IconUrl = ReadValueAndReplace(metadata, ns + "iconUrl", definition.AssemblyAttributes),
                    Tags = ReadValueAndReplace(metadata, ns + "tags", definition.AssemblyAttributes),
                    ReleaseNotes = ReadValueAndReplace(metadata, ns + "releaseNotes", definition.AssemblyAttributes),
                    RequiresLicenseAcceptance = metadata.Element(ns + "requireLicenseAcceptance")?.Value != null ? bool.Parse(metadata.Element(ns + "requireLicenseAcceptance")?.Value) : false
                };

                var dependencies = metadata.Element(ns + "dependencies")?.Elements(ns + "dependency");
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        var packageId = dependency.Attribute("id").Value;
                        var constraint = dependency.Attribute("version").Value;

                        var packageReference = definition.PackageReferences?.FirstOrDefault(x => x.Id.Equals(packageId, StringComparison.OrdinalIgnoreCase));
                        if (packageReference != null)
                        {
                            packageReference.Version = constraint;
                        }
                    }
                }
            }
        }

        private void ConvertVersionConstraints(XElement dependencies, IReadOnlyList<PackageReference> packageReferences)
        {
            throw new NotImplementedException();
        }

        private string ReadValueAndReplace(XElement metadata, XName elementName, AssemblyAttributes assemblyAttributes)
        {
            var value = metadata.Element(elementName)?.Value;
            if (value != null && assemblyAttributes != null)
            {
                value = value
                    .Replace("$id$", assemblyAttributes.AssemblyName ?? "$id$")
                    .Replace("$version$", assemblyAttributes.InformationalVersion ?? assemblyAttributes.Version ?? "$version$")
                    .Replace("$author$", assemblyAttributes.Company ?? "$author$")
                    .Replace("$description$", assemblyAttributes.Description ?? "$description$")
                    .Replace("$copyright$", assemblyAttributes.Copyright ?? "$copyright$");
            }

            return value;
        }
    }
}
