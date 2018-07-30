using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using static Project2015To2017.Definition.Project;

namespace Project2015To2017.Reading
{
	public class ProjectReader
	{
		public ProjectReader()
		{
		}

		public ProjectReader(FileInfo projectFile, IProgress<string> progress = null)
		{
			ProjectPath = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
			if (progress != null)
				ProgressReporter = progress;
		}

		public ProjectReader(string projectFilePath, IProgress<string> progress = null)
		{
			ProjectPath = new FileInfo(projectFilePath) ?? throw new ArgumentNullException(nameof(projectFilePath));
			if (progress != null)
				ProgressReporter = progress;
		}

		public static bool EnableCaching;

		public FileInfo ProjectPath { get; set; }

		public IProgress<string> ProgressReporter { get; set; } = new Progress<string>(_ => { });

		[Obsolete]
		public Project Read(string filePath, IProgress<string> progress = null)
		{
			ProjectPath = new FileInfo(filePath);
			if (progress != null)
				ProgressReporter = progress;

			return Read();
		}

		/// <summary>
		/// Process-lifetime-long cache of loaded projects
		/// </summary>
		private static readonly Dictionary<string, Project> _cache = new Dictionary<string, Project>();

		public static void PurgeCache()
		{
			_cache.Clear();
		}

		public Project Read(bool loadModern = false)
		{
			var filePath = ProjectPath.FullName;
			if (EnableCaching && _cache.TryGetValue(filePath, out var projectDefinition))
			{
				return projectDefinition;
			}

			XDocument projectXml;
			using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				projectXml = XDocument.Load(stream, LoadOptions.SetLineInfo);
			}

			// get ProjectTypeGuids and check for unsupported types
			if (UnsupportedProjectTypes.IsUnsupportedProjectType(projectXml))
			{
				ProgressReporter.Report("This project type is not supported for conversion.");
				return null;
			}

			var isLegacy = projectXml.Element(XmlLegacyNamespace + "Project") != null;
			if (!isLegacy)
			{
				ProgressReporter.Report($"This is not a VS2015 project file.");
				return null;
			}

			var packageConfig = new NuSpecReader().Read(ProjectPath, ProgressReporter);

			projectDefinition = new Project
			{
				FilePath = ProjectPath,
				PackageConfiguration = packageConfig,
				Deletions = Array.Empty<FileSystemInfo>(),
				AssemblyAttributeProperties = Array.Empty<XElement>()
			};

			if (EnableCaching)
			{
				_cache.Add(filePath, projectDefinition);
			}
			
			projectDefinition.AssemblyReferences = LoadAssemblyReferences(projectXml);
			projectDefinition.ProjectReferences = LoadProjectReferences(projectXml);
			projectDefinition.PackagesConfigFile = FindPackagesConfigFile(ProjectPath);
			projectDefinition.PackageReferences = LoadPackageReferences(projectXml, projectDefinition.PackagesConfigFile);
			projectDefinition.IncludeItems = LoadFileIncludes(projectXml);

			ProcessProjectReferences(projectDefinition);

			HandleSpecialProjectTypes(projectXml, projectDefinition);

			ProjectPropertiesReader.PopulateProperties(projectDefinition, projectXml);

			var assemblyAttributes = new AssemblyInfoReader().Read(projectDefinition, ProgressReporter);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition;
		}

		private void HandleSpecialProjectTypes(XContainer projectXml, Project projectDefinition)
		{
			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

			// get the MyType tag
			var outputType = projectXml.Descendants(nsSys + "MyType").FirstOrDefault();
			// WinForms applications
			if (outputType?.Value == "WindowsForms")
			{
				ProgressReporter.Report($"This is a Windows Forms project file, support is limited.");
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

			if (guidTypes.Contains("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"))
			{
				projectDefinition.TargetFrameworks.Add("xamarin.android");
			}

			if (guidTypes.Contains("{6BC8ED88-2882-458C-8E55-DFD12B67127B}"))
			{
				projectDefinition.TargetFrameworks.Add("xamarin.ios");
			}

			if (guidTypes.Contains("{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}"))
			{
				projectDefinition.TargetFrameworks.Add("uap");
			}

			if (guidTypes.Contains("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}"))
			{
				projectDefinition.IsWindowsPresentationFoundationProject = true;
			}
		}

		private static void ProcessProjectReferences(Project projectDefinition)
		{
			foreach (var reference in projectDefinition.ProjectReferences)
			{
				reference.ProjectFile = new FileInfo(Path.Combine(projectDefinition.FilePath.Directory.FullName, reference.Include));
			}
		}

		private FileInfo FindPackagesConfigFile(FileInfo projectFile)
		{
			var packagesConfig = new FileInfo(Path.Combine(projectFile.Directory.FullName, "packages.config"));

			if (!packagesConfig.Exists)
			{
				ProgressReporter.Report("Packages.config file not found.");
				return null;
			}
			else
			{
				return packagesConfig;
			}
		}

		private IReadOnlyList<PackageReference> LoadPackageReferences(XDocument projectXml, FileInfo packagesConfig)
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
					ProgressReporter.Report($"Found nuget reference to {reference.Id}, version {reference.Version}.");
				}

				return packageReferences;
			}
			catch (XmlException e)
			{
				ProgressReporter.Report($"Got xml exception reading packages.config: " + e.Message);
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

		private IReadOnlyList<ProjectReference> LoadProjectReferences(XDocument projectXml)
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

		private List<AssemblyReference> LoadAssemblyReferences(XDocument projectXml)
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
					SpecificVersion = specificVersion,
					DefinitionElement = referenceElement,
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