using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Reading
{
	public class NuSpecReader
	{
		public PackageConfiguration Read(FileInfo projectFile, IProgress<string> progress)
		{
			var nuspecFiles = projectFile.Directory
										 .EnumerateFiles("*.nuspec", SearchOption.TopDirectoryOnly)
										 .Where(x => !x.FullName.Contains(@"\obj\"))										
										 .ToArray();

			if (nuspecFiles.Length == 0)
			{
				progress.Report("No nuspec found, skipping package configuration.");
				return null;
			}

			if (nuspecFiles.Length > 1)
			{
				progress.Report($@"Could not read from nuspec, multiple nuspecs found: 
{string.Join(Environment.NewLine, nuspecFiles.Select(x => x.FullName))}.");
				return null;
			}

			var nuspecFile = nuspecFiles[0];

			progress.Report($"Reading package info from nuspec {nuspecFile.FullName}.");

			XDocument nuspec;
			using (var filestream = File.Open(nuspecFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				nuspec = XDocument.Load(filestream);
			}

			var namespaces = new XNamespace[]
			{
				"",
				"http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd",
				"http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd",
				"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
			};

			var packageConfig = namespaces
									.Select(ns => ExtractPackageConfiguration(nuspec, ns))
									.SingleOrDefault(config => config != null);

			if (packageConfig == null)
			{
				progress.Report("Error reading package info from nuspec.");
				return null;
			}

			packageConfig.NuspecFile = nuspecFile;

			return packageConfig;
		}

		private PackageConfiguration ExtractPackageConfiguration(
				XDocument nuspec, XNamespace ns
			)
		{
			var metadata = nuspec?.Element(ns + "package")?.Element(ns + "metadata");

			if (metadata == null)
			{
				return null;
			}

			var id = metadata.Element(ns + "id")?.Value;

			var version = metadata.Element(ns + "version")?.Value;

			var dependencies = metadata.Element(ns + "dependencies")
									   ?.Elements(ns + "dependency")
										.ToList();

			var packageConfig = new PackageConfiguration {
				Id = id,
				Version = version,
				Authors = GetElement(metadata, ns + "authors"),
				Description = GetElement(metadata, ns + "description"),
				Copyright = GetElement(metadata, ns + "copyright"),
				LicenseUrl = GetElement(metadata, ns + "licenseUrl"),
				ProjectUrl = GetElement(metadata, ns + "projectUrl"),
				IconUrl = GetElement(metadata, ns + "iconUrl"),
				Tags = GetElement(metadata, ns + "tags"),
				ReleaseNotes = GetElement(metadata, ns + "releaseNotes"),
				RequiresLicenseAcceptance = metadata.Element(ns + "requireLicenseAcceptance")?.Value != null && bool.Parse(metadata.Element(ns + "requireLicenseAcceptance")?.Value),
				Dependencies = dependencies
			};

			return packageConfig;
		}

		private static string GetElement(XElement metadata, XName elementName)
		{
			var value = metadata.Element(elementName)?.Value;

			return value;
		}
	}
}
