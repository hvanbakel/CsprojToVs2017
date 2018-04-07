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
										 .EnumerateFiles("*.nuspec", SearchOption.AllDirectories)
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

			var packageConfig = namespaces
									.Select(ns => ExtractPackageConfiguration(nuspec, ns))
									.SingleOrDefault(config => config != null);

			if (packageConfig == null)
			{
				progress.Report("Error reading package info from nuspec.");
				return null;
			}

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
			if (id == "$id$")
			{
				id = null;
			}

			var version = metadata.Element(ns + "version")?.Value;
			if (version == "$version$")
			{
				version = null;
			}

			var dependencies = metadata.Element(ns + "dependencies")?.Elements(ns + "dependency");

			var packageConfig = new PackageConfiguration(
				id : id,
				version : version,
				authors : GetElement(metadata, ns + "authors"),
				description : GetElement(metadata, ns + "description"),
				copyright : GetElement(metadata, ns + "copyright"),
				licenseUrl : GetElement(metadata, ns + "licenseUrl"),
				projectUrl : GetElement(metadata, ns + "projectUrl"),
				iconUrl : GetElement(metadata, ns + "iconUrl"),
				tags : GetElement(metadata, ns + "tags"),
				releaseNotes : GetElement(metadata, ns + "releaseNotes"),
				requiresLicenseAcceptance : metadata.Element(ns + "requireLicenseAcceptance")?.Value != null && bool.Parse(metadata.Element(ns + "requireLicenseAcceptance")?.Value),
				dependencies: dependencies
			);

			return packageConfig;
		}

		private static string GetElement(XElement metadata, XName elementName)
		{
			var value = metadata.Element(elementName)?.Value;

			return value;
		}
	}
}