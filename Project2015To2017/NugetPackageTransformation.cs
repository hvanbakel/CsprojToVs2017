using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017
{
    internal sealed class NugetPackageTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            var nuspecFiles = projectFolder
                .EnumerateFiles("*.nuspec", SearchOption.AllDirectories)
                .ToArray();

            if (nuspecFiles.Length == 0)
            {
                Console.WriteLine("No nuspec found, skipping package configuration.");
            }
            else if (nuspecFiles.Length == 1)
            {
                Console.WriteLine($"Reading package info from nuspec {nuspecFiles[0].FullName}.");

                XDocument nuspec;
                using (var filestream = File.Open(nuspecFiles[0].FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    nuspec = XDocument.Load(filestream);
                }

                XNamespace ns = "http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd";
                ExtractPackageConfiguration(definition, nuspec, ns);
            }
            else
            {
                Console.WriteLine($@"Could not read from nuspec, multiple nuspecs found: 
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
            }
            else
            {
                Console.WriteLine("Error reading package info from nuspec.");
            }
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
