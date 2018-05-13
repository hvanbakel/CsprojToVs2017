using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
			return Read(filePath, new Progress<string>(_ => {}));
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
				PackagesConfigFile = packagesConfigFile
			};

			ProjectPropertiesReader.PopulateProperties(projectDefinition, projectXml);

			var assemblyAttributes = LoadAssemblyAttributes(fileInfo, progress);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition;
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

		private List<PackageReference> LoadPackageReferences(XDocument projectXml, FileInfo packagesConfig, IProgress<string> progress)
		{
			var packageReferences = new List<PackageReference>();

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

				var packageConfigPackages = PackageConfigPackages(packagesConfig);


				packageReferences = packageConfigPackages
										.Concat(existingPackageReferences)
										.ToList();

				foreach (var reference in packageReferences)
				{
					progress.Report($"Found nuget reference to {reference.Id}, version {reference.Version}.");
				}
			}
			catch (XmlException e)
			{
				progress.Report($"Got xml exception reading packages.config: " + e.Message);
			}

			return packageReferences;
		}

		private static IEnumerable<PackageReference> PackageConfigPackages(FileInfo packagesConfig)
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


		private AssemblyAttributes LoadAssemblyAttributes(
				FileInfo projectFile, IProgress<string> progress
			)
		{
			var projectFolder = projectFile.Directory;

			var assemblyInfoFiles = projectFolder
										.EnumerateFiles("AssemblyInfo.cs", SearchOption.AllDirectories)
										.ToArray();

			if (assemblyInfoFiles.Length == 1)
			{
				progress.Report($"Reading assembly info from {assemblyInfoFiles[0].FullName}.");

				var text = File.ReadAllText(assemblyInfoFiles[0].FullName);

				return new AssemblyAttributes
				{
					Description = GetAttributeValue<AssemblyDescriptionAttribute>(text),
					Title = GetAttributeValue<AssemblyTitleAttribute>(text),
					Company = GetAttributeValue<AssemblyCompanyAttribute>(text),
					Product = GetAttributeValue<AssemblyProductAttribute>(text),
					Copyright = GetAttributeValue<AssemblyCopyrightAttribute>(text),
					InformationalVersion = GetAttributeValue<AssemblyInformationalVersionAttribute>(text),
					Version = GetAttributeValue<AssemblyVersionAttribute>(text),
					FileVersion = GetAttributeValue<AssemblyFileVersionAttribute>(text),
					Configuration = GetAttributeValue<AssemblyConfigurationAttribute>(text)
				};
			}
			else
			{
				progress.Report($@"Could not read from assemblyinfo, multiple assemblyinfo files found: 
{string.Join(Environment.NewLine, assemblyInfoFiles.Select(x => x.FullName))}.");
			}

			return null;
		}

		private string GetAttributeValue<T>(string text)
			where T : Attribute
		{
			var attributeTypeName = typeof(T).Name;
			var attributeName = attributeTypeName.Substring(0, attributeTypeName.Length - 9);

			var regex = new Regex($@"\[assembly:.*{attributeName}\(\""(?<value>.*)\""\)]", RegexOptions.Compiled);

			// TODO parse this in roslyn so we actually know that it's not comments.
			var match = regex.Match(text);
			if (match.Groups.Count > 1)
			{
				return match.Groups[1].Value;
			}
			return null;
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

				var hintPath = GetElementValue(referenceElement, "HintPath"); ;

				var isPrivate = GetElementValue(referenceElement, "Private");

				var embedInteropTypes = GetElementValue(referenceElement, "EmbedInteropTypes");

				var output = new AssemblyReference {
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