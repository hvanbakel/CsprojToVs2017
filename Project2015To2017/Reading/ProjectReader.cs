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

			XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
			if (projectXml.Element(nsSys + "Project") == null)
			{
				progress.Report($"This is not a VS2015 project file.");
				return null;
			}

			var fileInfo = new FileInfo(filePath);
			
			var assemblyReferences = LoadAssemblyReferences(projectXml, progress);
			var projectReferences = LoadProjectReferences(projectXml, progress);
			var packageReferences = LoadPackageReferences(fileInfo, projectXml, progress);

			var includes = LoadFileIncludes(projectXml);

			var packageConfig = new NuSpecReader().Read(fileInfo, progress);

			var projectDefinition = new ProjectBuilder
			{
				FilePath = fileInfo,
				AssemblyReferences = assemblyReferences,
				ProjectReferences = projectReferences,
				PackageReferences = packageReferences,
				IncludeItems = includes,
				PackageConfiguration = packageConfig
			};

			//todo: change this to use a pure method like the other loaders
			//probably by collecting properties into a class
			ProjectPropertiesReader.PopulateProperties(projectDefinition, projectXml);

			var assemblyAttributes = LoadAssemblyAttributes(fileInfo, projectDefinition.AssemblyName, progress);

			projectDefinition.AssemblyAttributes = assemblyAttributes;

			return projectDefinition.ToImmutable();
		}

		private IReadOnlyList<PackageReference> LoadPackageReferences(FileInfo projectFile, XDocument projectXml, IProgress<string> progress)
		{
			var packagesConfig = projectFile.Directory.GetFiles("packages.config", SearchOption.TopDirectoryOnly);

			var packageReferences = new List<PackageReference>();

			if (packagesConfig == null || packagesConfig.Length == 0)
			{
				progress.Report("Packages.config file not found.");
			}

			try
			{
				var existingPackageReferences = projectXml.Root.Elements(XmlNamespace + "ItemGroup")
															   .Elements(XmlNamespace + "PackageReference")
															   .Select(x => new PackageReference
																(
																	id : x.Attribute("Include").Value,
																	version : x.Attribute("Version")?.Value ?? x.Element(XmlNamespace + "Version").Value,
																	isDevelopmentDependency : x.Element(XmlNamespace + "PrivateAssets") != null
																));

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

			return packageReferences.AsReadOnly();
		}

		private static IEnumerable<PackageReference> PackageConfigPackages(FileInfo[] packagesConfig)
		{
			if (packagesConfig == null || !packagesConfig.Any())
			{
				return Enumerable.Empty<PackageReference>();
			}

			XDocument packagesConfigDoc;
			using (var stream = File.Open(packagesConfig[0].FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				packagesConfigDoc = XDocument.Load(stream);
			}

			var packageConfigPackages = packagesConfigDoc.Element("packages").Elements("package")
				.Select(x => new PackageReference
				(
					id: x.Attribute("id").Value,
					version: x.Attribute("version").Value,
					isDevelopmentDependency: x.Attribute("developmentDependency")?.Value == "true"
				));

			return packageConfigPackages;
		}

		private IReadOnlyList<ProjectReference> LoadProjectReferences(XDocument projectXml, IProgress<string> progress)
		{
			var projectReferences = projectXml
									.Element(XmlNamespace + "Project")
									.Elements(XmlNamespace + "ItemGroup")
									.Elements(XmlNamespace + "ProjectReference")
									.Select(x => new ProjectReference
									(
										include : x.Attribute("Include").Value,
										aliases : x.Element(XmlNamespace + "Aliases")?.Value
									))
									.ToList().AsReadOnly();

			return projectReferences;
		}


		private AssemblyAttributes LoadAssemblyAttributes(
				FileInfo projectFile, string assemblyName, IProgress<string> progress
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

				return new AssemblyAttributes(
					assemblyName: assemblyName ?? projectFolder.Name,
					description: GetAttributeValue<AssemblyDescriptionAttribute>(text),
					title: GetAttributeValue<AssemblyTitleAttribute>(text),
					company: GetAttributeValue<AssemblyCompanyAttribute>(text),
					product: GetAttributeValue<AssemblyProductAttribute>(text),
					copyright: GetAttributeValue<AssemblyCopyrightAttribute>(text),
					informationalVersion: GetAttributeValue<AssemblyInformationalVersionAttribute>(text),
					version: GetAttributeValue<AssemblyVersionAttribute>(text),
					fileVersion: GetAttributeValue<AssemblyFileVersionAttribute>(text),
					configuration: GetAttributeValue<AssemblyConfigurationAttribute>(text)
				);
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

				var output = new AssemblyReference(
					include,
					embedInteropTypes,
					hintPath,
					isPrivate,
					specificVersion
				);

				return output;
			}
		}

		private static string GetElementValue(XElement reference, string elementName)
		{
			var element = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == elementName);

			return element?.Value;
		}
		private static IReadOnlyList<XElement> LoadFileIncludes(XDocument projectXml)
		{
			var items = projectXml
								?.Element(XmlNamespace + "Project")
								?.Elements(XmlNamespace + "ItemGroup")
								.ToList().AsReadOnly()

			                 ?? new List<XElement>().AsReadOnly();

			return items;
		}
	}
}