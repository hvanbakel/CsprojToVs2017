using Microsoft.Extensions.Logging;
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
		private readonly Caching.IProjectCache projectCache;
		private readonly NuSpecReader nuspecReader;
		private readonly AssemblyInfoReader assemblyInfoReader;
		private readonly ProjectPropertiesReader projectPropertiesReader;
		private readonly ILogger logger;

		public ProjectReader(ILogger logger = null, ConversionOptions conversionOptions = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
			this.projectCache = conversionOptions?.ProjectCache ?? Caching.NoProjectCache.Instance;
			this.nuspecReader = new NuSpecReader(this.logger);
			this.assemblyInfoReader = new AssemblyInfoReader(this.logger);
			this.projectPropertiesReader = new ProjectPropertiesReader(this.logger);
		}

		public Project Read(string projectFilePath)
		{
			projectFilePath = projectFilePath ?? throw new ArgumentNullException(nameof(projectFilePath));
			return Read(new FileInfo(projectFilePath));
		}

		public Project Read(FileInfo projectFile)
		{
			projectFile = projectFile ?? throw new ArgumentNullException(nameof(projectFile));

			var filePath = projectFile.FullName;
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
				this.logger.LogWarning("This is not a MSBuild (Visual Studio) project file.");
				return null;
			}

			var packageConfig = this.nuspecReader.Read(projectFile);

			projectDefinition = new Project
			{
				IsModernProject = isModern,
				FilePath = projectFile,
				ProjectDocument = projectXml,
				PackageConfiguration = packageConfig,
				Deletions = Array.Empty<FileSystemInfo>(),
				AssemblyAttributeProperties = Array.Empty<XElement>()
			};

			// get ProjectTypeGuids and check for unsupported types
			if (UnsupportedProjectTypes.IsUnsupportedProjectType(projectDefinition))
			{
				this.logger.LogError("This project type is not supported for conversion.");
				return null;
			}

			this.projectCache.Add(filePath, projectDefinition);

			projectDefinition.ProjectGuid = ReadProjectGuid(projectDefinition);
			projectDefinition.AssemblyReferences = LoadAssemblyReferences(projectDefinition);
			projectDefinition.ProjectReferences = LoadProjectReferences(projectDefinition);
			projectDefinition.PackagesConfigFile = FindPackagesConfigFile(projectFile);
			projectDefinition.PackageReferences = LoadPackageReferences(projectDefinition);
			projectDefinition.ItemGroups = LoadFileIncludes(projectDefinition);

			ProcessProjectReferences(projectDefinition);

			HandleSpecialProjectTypes(projectDefinition);

			projectPropertiesReader.Read(projectDefinition);

			var assemblyAttributes = this.assemblyInfoReader.Read(projectDefinition);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition;
		}

		private Guid? ReadProjectGuid(Project projectDefinition)
		{
			var projectTypeNode = projectDefinition
				.ProjectDocument
				.Descendants(projectDefinition.XmlNamespace + "ProjectGuid")
				.FirstOrDefault();

			return projectTypeNode != null
				? Guid.Parse(projectTypeNode.Value)
				: default(Guid?);
		}

		private void HandleSpecialProjectTypes(Project project)
		{
			// get the MyType tag
			var outputType = project.ProjectDocument
				.Descendants(project.XmlNamespace + "MyType")
				.FirstOrDefault();
			// WinForms applications
			if (outputType?.Value == "WindowsForms")
			{
				this.logger.LogWarning("This is a Windows Forms project file, support is limited.");
				project.IsWindowsFormsProject = true;
			}

			// try to get project type - may not exist
			var typeElement = project.ProjectDocument
				.Descendants(project.XmlNamespace + "ProjectTypeGuids")
				.FirstOrDefault();
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
				if (reference.ProjectFile != null)
				{
					continue;
				}

				var path = reference.Include;
				var projectDirectory = projectDefinition.FilePath.DirectoryName;
				var adjustedPath = Extensions.MaybeAdjustFilePath(path, projectDirectory);
				reference.ProjectFile = new FileInfo(Path.Combine(projectDirectory, adjustedPath));
			}
		}

		private FileInfo FindPackagesConfigFile(FileInfo projectFile)
		{
			var packagesConfig = new FileInfo(Path.Combine(projectFile.DirectoryName, "packages.config"));

			if (!packagesConfig.Exists)
			{
				this.logger.LogInformation("Packages.config file not found.");
				return null;
			}

			return packagesConfig;
		}

		private IReadOnlyList<PackageReference> LoadPackageReferences(Project project)
		{
			try
			{
				var existingPackageReferences = project.ProjectDocument.Root
					.Elements(project.XmlNamespace + "ItemGroup")
					.Elements(project.XmlNamespace + "PackageReference")
					.Select(x => new PackageReference
					{
						Id = x.Attribute("Include").Value,
						Version = x.Attribute("Version")?.Value ?? x.Element(project.XmlNamespace + "Version").Value,
						IsDevelopmentDependency = x.Element(project.XmlNamespace + "PrivateAssets") != null,
						DefinitionElement = x
					});

				var packageConfigPackages = ExtractReferencesFromPackagesConfig(project.PackagesConfigFile);


				var packageReferences = packageConfigPackages
					.Concat(existingPackageReferences)
					.ToList();

				foreach (var reference in packageReferences)
				{
					this.logger.LogDebug($"Found nuget reference to {reference.Id}, version {reference.Version}.");
				}

				return packageReferences;
			}
			catch (XmlException e)
			{
				this.logger.LogError($"Got xml exception reading packages.config: " + e.Message);
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
			var projectReferences = project.ProjectDocument.Root
				.Elements(project.XmlNamespace + "ItemGroup")
				.Elements(project.XmlNamespace + "ProjectReference")
				.Select(CreateProjectReference)
				.ToList();

			return projectReferences;

			ProjectReference CreateProjectReference(XElement x)
			{
				var projectGuid = x.Element(project.XmlNamespace + "Project")?.Value;
				var embedInteropTypes = x.Element(project.XmlNamespace + "EmbedInteropTypes")?.Value;

				var reference = new ProjectReference
				{
					Include = x.Attribute("Include").Value,
					ProjectName = x.Element(project.XmlNamespace + "Name")?.Value,
					Aliases = x.Element(project.XmlNamespace + "Aliases")?.Value,
					EmbedInteropTypes = string.Equals(embedInteropTypes, "true", StringComparison.OrdinalIgnoreCase),
					DefinitionElement = x
				};

				if (!string.IsNullOrEmpty(projectGuid))
				{
					reference.ProjectGuid = Guid.Parse(projectGuid);
				}

				return reference;
			}
		}

		private List<AssemblyReference> LoadAssemblyReferences(Project project)
		{
			return project.ProjectDocument.Root
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