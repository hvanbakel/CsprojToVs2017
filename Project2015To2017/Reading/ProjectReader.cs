using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Project2015To2017.Definition;
using static Project2015To2017.Definition.Project;

namespace Project2015To2017.Reading
{
	public class ProjectReader
	{
		public Project Read(string filePath)
		{
			return Read(filePath, new Progress<string>(_ => { }));
		}

		public Project Read(string filePath, IProgress<string> progress)
		{
			XDocument projectXml;
			using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				projectXml = XDocument.Load(stream);
			}

			// get ProjectTypeGuids and check for unsupported types
			if (UnsupportedProjectTypes.IsUnsupportedProjectType(projectXml))
			{
				progress.Report("This project type is not supported for conversion.");
				return null;
			}

			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
			if (projectXml.Element(nsSys + "Project") == null)
			{
				progress.Report($"This is not a VS2015 project file.");
				return null;
			}

			var fileInfo = new FileInfo(filePath);

			var assemblyReferences = LoadAssemblyReferences(projectXml, progress);
			var projectReferences = LoadProjectReferences(projectXml, progress);

			var packagesConfigFile = FindPackagesConfigFile(fileInfo, progress);

			var packageReferences = LoadPackageReferences(projectXml, packagesConfigFile, progress);

			var includes = LoadFileIncludes(projectXml);

			var packageConfig = new NuSpecReader().Read(fileInfo, progress);

			var projectDefinition = new Project
			{
				FilePath = fileInfo,
				AssemblyReferences = assemblyReferences,
				ProjectReferences = projectReferences,
				PackageReferences = packageReferences,
				IncludeItems = includes,
				PackageConfiguration = packageConfig,
				PackagesConfigFile = packagesConfigFile,
				Deletions = Array.Empty<FileSystemInfo>(),
				AssemblyAttributeProperties = Array.Empty<XElement>()
			};

			HandleSpecialProjectTypes(progress, projectXml, projectDefinition);

			ProjectPropertiesReader.PopulateProperties(projectDefinition, projectXml);

			var assemblyAttributes = new AssemblyInfoReader().Read(projectDefinition, progress);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition;
		}

		private static void HandleSpecialProjectTypes(IProgress<string> progress, XContainer projectXml,
			Project projectDefinition)
		{
			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

			// get the MyType tag
			var outputType = projectXml.Descendants(nsSys + "MyType").FirstOrDefault();
			// WinForms applications
			if (outputType?.Value == "WindowsForms")
			{
				progress.Report($"This is a Windows Forms project file, support is limited.");
				projectDefinition.IsWindowsFormsProject = true;
			}

			// try to get project type - may not exist
			var typeElement = projectXml.Descendants(nsSys + "ProjectTypeGuids").FirstOrDefault();
			if (typeElement == null) return;

			// parse the CSV list
			var guidTypes = typeElement.Value
				.Split(';')
				.Select(x => x.Trim().ToUpperInvariant())
				.ToImmutableHashSet();

			// can be enabled in separate PR when simplification PR is merged
#if false
			if (guidTypes.Contains("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"))
				projectDefinition.TargetFrameworks.Add("xamarin.android");

			if (guidTypes.Contains("{6BC8ED88-2882-458C-8E55-DFD12B67127B}"))
				projectDefinition.TargetFrameworks.Add("xamarin.ios");

			if (guidTypes.Contains("{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}"))
				projectDefinition.TargetFrameworks.Add("uap");
#endif

			if (guidTypes.Contains("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}"))
				projectDefinition.IsWindowsPresentationFoundationProject = true;
		}

		private FileInfo FindPackagesConfigFile(FileInfo projectFile, IProgress<string> progress)
		{
			var packagesConfig = new FileInfo(Path.Combine(projectFile.Directory.FullName, "packages.config"));

			if (!packagesConfig.Exists)
			{
				progress.Report("Packages.config file not found.");
				return null;
			}
			else
			{
				return packagesConfig;
			}
		}

		private IReadOnlyList<PackageReference> LoadPackageReferences(XDocument projectXml, FileInfo packagesConfig,
			IProgress<string> progress)
		{
			try
			{
				var existingPackageReferences = projectXml.Root.Elements(XmlNamespace + "ItemGroup")
					.Elements(XmlNamespace + "PackageReference")
					.Select(x => new PackageReference
					{
						Id = x.Attribute("Include").Value,
						Version = x.Attribute("Version")?.Value ?? x.Element(XmlNamespace + "Version").Value,
						IsDevelopmentDependency = x.Element(XmlNamespace + "PrivateAssets") != null
					});

				var packageConfigPackages = ExtractReferencesFromPackagesConfig(packagesConfig);


				var packageReferences = packageConfigPackages
					.Concat(existingPackageReferences)
					.ToList();

				foreach (var reference in packageReferences)
				{
					progress.Report($"Found nuget reference to {reference.Id}, version {reference.Version}.");
				}

				return packageReferences;
			}
			catch (XmlException e)
			{
				progress.Report($"Got xml exception reading packages.config: " + e.Message);
			}

			return Array.Empty<PackageReference>();
		}

		private static IEnumerable<PackageReference> ExtractReferencesFromPackagesConfig(FileInfo packagesConfig)
		{
			if (packagesConfig == null)
			{
				return Enumerable.Empty<PackageReference>();
			}

			XDocument packagesConfigDoc;
			using (var stream = File.Open(packagesConfig.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				packagesConfigDoc = XDocument.Load(stream);
			}

			var packageConfigPackages = packagesConfigDoc.Element("packages").Elements("package")
				.Select(x => new PackageReference
				{
					Id = x.Attribute("id").Value,
					Version = x.Attribute("version").Value,
					IsDevelopmentDependency = x.Attribute("developmentDependency")?.Value == "true"
				});

			return packageConfigPackages;
		}

		private IReadOnlyList<ProjectReference> LoadProjectReferences(XDocument projectXml, IProgress<string> progress)
		{
			var projectReferences = projectXml
				.Element(XmlNamespace + "Project")
				.Elements(XmlNamespace + "ItemGroup")
				.Elements(XmlNamespace + "ProjectReference")
				.Select(x => new ProjectReference
				{
					Include = x.Attribute("Include").Value,
					Aliases = x.Element(XmlNamespace + "Aliases")?.Value
				})
				.ToList();

			return projectReferences;
		}

		private List<AssemblyReference> LoadAssemblyReferences(XDocument projectXml, IProgress<string> progress)
		{
			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

			return projectXml
				.Element(nsSys + "Project")
				?.Elements(nsSys + "ItemGroup")
				.Elements(nsSys + "Reference")
				.Select(FormatAssemblyReference)
				.ToList();

			AssemblyReference FormatAssemblyReference(XElement referenceElement)
			{
				var include = referenceElement.Attribute("Include")?.Value;

				var specificVersion = GetElementValue(referenceElement, "SpecificVersion");

				var hintPath = GetElementValue(referenceElement, "HintPath");

				var isPrivate = GetElementValue(referenceElement, "Private");

				var embedInteropTypes = GetElementValue(referenceElement, "EmbedInteropTypes");

				var output = new AssemblyReference
				{
					Include = include,
					EmbedInteropTypes = embedInteropTypes,
					HintPath = hintPath,
					Private = isPrivate,
					SpecificVersion = specificVersion
				};

				return output;
			}
		}

		private static string GetElementValue(XElement reference, string elementName)
		{
			var element = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == elementName);

			return element?.Value;
		}

		private static List<XElement> LoadFileIncludes(XDocument projectXml)
		{
			var items = projectXml
				            ?.Element(XmlNamespace + "Project")
				            ?.Elements(XmlNamespace + "ItemGroup")
				            .ToList()
			            ?? new List<XElement>();

			return items;
		}
	}
}