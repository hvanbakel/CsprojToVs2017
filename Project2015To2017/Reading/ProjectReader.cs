using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Project2015To2017.Reading
{
	public sealed class ProjectReader
	{
		private readonly FileInfo projectPath;
		private readonly Caching.IProjectCache projectCache;
		private readonly IProgress<string> progressReporter;

		public ProjectReader(FileInfo projectFile, IProgress<string> progress = null, ConversionOptions conversionOptions = null)
			: this(progress, conversionOptions)
		{
			this.projectPath = projectFile ?? throw new ArgumentNullException(nameof(projectFile));
		}

		public ProjectReader(string projectFilePath, IProgress<string> progress = null, ConversionOptions conversionOptions = null)
			: this(progress, conversionOptions)
		{
			projectFilePath = projectFilePath ?? throw new ArgumentNullException(nameof(projectFilePath));
			this.projectPath = new FileInfo(projectFilePath);
		}

		private ProjectReader(IProgress<string> progress, ConversionOptions conversionOptions)
		{
			this.progressReporter = progress ?? new Progress<string>(_ => { });
			this.projectCache = conversionOptions?.ProjectCache ?? Caching.NoProjectCache.Instance;
		}

		public Project Read()
		{
			var filePath = this.projectPath.FullName;
			if (this.projectCache.TryGetValue(filePath, out var projectDefinition))
			{
				return projectDefinition;
			}

			XDocument projectXml;
			using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				projectXml = XDocument.Load(stream, LoadOptions.SetLineInfo);
			}

			var isLegacy = projectXml.Element(Project.XmlLegacyNamespace + "Project") != null;
			var isModern = projectXml.Element(XNamespace.None + "Project") != null;
			if (!isModern && !isLegacy)
			{
				progressReporter.Report("This is not a MSBuild (Visual Studio) project file.");
				return null;
			}

			var packageConfig = new NuSpecReader().Read(projectPath, progressReporter);

			projectDefinition = new Project
			{
				IsModernProject = isModern,
				FilePath = projectPath,
				ProjectDocument = projectXml,
				PackageConfiguration = packageConfig,
				Deletions = Array.Empty<FileSystemInfo>(),
				AssemblyAttributeProperties = Array.Empty<XElement>()
			};

			// get ProjectTypeGuids and check for unsupported types
			if (UnsupportedProjectTypes.IsUnsupportedProjectType(projectDefinition))
			{
				progressReporter.Report("This project type is not supported for conversion.");
				return null;
			}

			this.projectCache.Add(filePath, projectDefinition);

			projectDefinition.AssemblyReferences = LoadAssemblyReferences(projectDefinition);
			projectDefinition.ProjectReferences = LoadProjectReferences(projectDefinition);
			projectDefinition.PackagesConfigFile = FindPackagesConfigFile(projectPath);
			projectDefinition.PackageReferences = LoadPackageReferences(projectDefinition);
			projectDefinition.ItemGroups = LoadFileIncludes(projectDefinition);

			ProcessProjectReferences(projectDefinition);

			HandleSpecialProjectTypes(projectXml, projectDefinition);

			ProjectPropertiesReader.PopulateProperties(projectDefinition, projectXml);

			var assemblyAttributes = new AssemblyInfoReader().Read(projectDefinition, progressReporter);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition;
		}

		private void HandleSpecialProjectTypes(XContainer projectXml, Project project)
		{
			// get the MyType tag
			var outputType = projectXml.Descendants(project.XmlNamespace + "MyType").FirstOrDefault();
			// WinForms applications
			if (outputType?.Value == "WindowsForms")
			{
				progressReporter.Report($"This is a Windows Forms project file, support is limited.");
				project.IsWindowsFormsProject = true;
			}

			// try to get project type - may not exist
			var typeElement = projectXml.Descendants(project.XmlNamespace + "ProjectTypeGuids").FirstOrDefault();
			if (typeElement == null)
			{
				return;
			}

			// parse the CSV list
			var guidTypes = typeElement.Value
				.Split(';')
				.Select(x => x.Trim().ToUpperInvariant())
				.ToImmutableHashSet();

			if (guidTypes.Contains("{EFBA0AD7-5A72-4C68-AF49-83D382785DCF}"))
			{
				project.TargetFrameworks.Add("xamarin.android");
			}

			if (guidTypes.Contains("{6BC8ED88-2882-458C-8E55-DFD12B67127B}"))
			{
				project.TargetFrameworks.Add("xamarin.ios");
			}

			if (guidTypes.Contains("{A5A43C5B-DE2A-4C0C-9213-0A381AF9435A}"))
			{
				project.TargetFrameworks.Add("uap");
			}

			if (guidTypes.Contains("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}"))
			{
				project.IsWindowsPresentationFoundationProject = true;
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
				progressReporter.Report("Packages.config file not found.");
				return null;
			}
			else
			{
				return packagesConfig;
			}
		}

		private IReadOnlyList<PackageReference> LoadPackageReferences(Project project)
		{
			var projectXml = project.ProjectDocument;
			var packagesConfig = project.PackagesConfigFile;
			try
			{
				var existingPackageReferences = projectXml.Root.Elements(project.XmlNamespace + "ItemGroup")
					.Elements(project.XmlNamespace + "PackageReference")
					.Select(x => new PackageReference
					{
						Id = x.Attribute("Include").Value,
						Version = x.Attribute("Version")?.Value ?? x.Element(project.XmlNamespace + "Version").Value,
						IsDevelopmentDependency = x.Element(project.XmlNamespace + "PrivateAssets") != null,
						DefinitionElement = x
					});

				var packageConfigPackages = ExtractReferencesFromPackagesConfig(packagesConfig);


				var packageReferences = packageConfigPackages
					.Concat(existingPackageReferences)
					.ToList();

				foreach (var reference in packageReferences)
				{
					progressReporter.Report($"Found nuget reference to {reference.Id}, version {reference.Version}.");
				}

				return packageReferences;
			}
			catch (XmlException e)
			{
				progressReporter.Report($"Got xml exception reading packages.config: " + e.Message);
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

		private IReadOnlyList<ProjectReference> LoadProjectReferences(Project project)
		{
			var projectReferences = project.ProjectDocument
				.Element(project.XmlNamespace + "Project")
				.Elements(project.XmlNamespace + "ItemGroup")
				.Elements(project.XmlNamespace + "ProjectReference")
				.Select(x => new ProjectReference
				{
					Include = x.Attribute("Include").Value,
					Aliases = x.Element(project.XmlNamespace + "Aliases")?.Value,
					EmbedInteropTypes = string.Equals(x.Element(project.XmlNamespace + "EmbedInteropTypes")?.Value, "true", StringComparison.OrdinalIgnoreCase),
					DefinitionElement = x
				})
				.ToList();

			return projectReferences;
		}

		private List<AssemblyReference> LoadAssemblyReferences(Project project)
		{
			return project.ProjectDocument
				.Element(project.XmlNamespace + "Project")
				?.Elements(project.XmlNamespace + "ItemGroup")
				.Elements(project.XmlNamespace + "Reference")
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

		private static List<XElement> LoadFileIncludes(Project project)
		{
			var items = project.ProjectDocument
							?.Element(project.XmlNamespace + "Project")
							?.Elements(project.XmlNamespace + "ItemGroup")
							.ToList()
						?? new List<XElement>();

			return items;
		}
	}
}