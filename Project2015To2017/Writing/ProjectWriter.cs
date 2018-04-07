using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Writing
{
	public class ProjectWriter
    {
        public void Write(Project project)
        {
            var projectNode = CreateXml(project, project.FilePath);

            using (var filestream = File.Open(project.FilePath.FullName, FileMode.Create))
            using (var streamWriter = new StreamWriter(filestream, Encoding.UTF8))
            {
                streamWriter.Write(projectNode.ToString());
            }
        }

        internal XElement CreateXml(Project project, FileInfo outputFile)
        {
            var projectNode = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"));

            projectNode.Add(GetMainPropertyGroup(project, outputFile));

            if (project.AdditionalPropertyGroups != null)
            {
                projectNode.Add(project.AdditionalPropertyGroups.Select(RemoveAllNamespaces));
            }

            if (project.Imports != null)
            {
                foreach (var import in project.Imports.Select(RemoveAllNamespaces))
                {
                    projectNode.Add(import);
                }
			}

			if (project.Targets != null)
			{
				foreach (var target in project.Targets.Select(RemoveAllNamespaces))
				{
					projectNode.Add(target);
				}
			}

			if (project.BuildEvents != null)
			{
				var propertyGroup = new XElement("PropertyGroup");
				projectNode.Add(propertyGroup);
				foreach (var buildEvent in project.BuildEvents.Select(RemoveAllNamespaces))
				{
					propertyGroup.Add(buildEvent);
				}
			}

			if (project.ProjectReferences?.Count > 0)
            {
                var itemGroup = new XElement("ItemGroup");
                foreach (var projectReference in project.ProjectReferences)
                {
                    var projectReferenceElement = new XElement("ProjectReference",
                            new XAttribute("Include", projectReference.Include));

                    if (!string.IsNullOrWhiteSpace(projectReference.Aliases) && projectReference.Aliases != "global")
                    {
                        projectReferenceElement.Add(new XElement("Aliases", projectReference.Aliases));
                    }

                    itemGroup.Add(projectReferenceElement);
                }

                projectNode.Add(itemGroup);
            }

            if (project.PackageReferences?.Count > 0)
            {
                var nugetReferences = new XElement("ItemGroup");
                foreach (var packageReference in project.PackageReferences)
                {
                    var reference = new XElement("PackageReference", new XAttribute("Include", packageReference.Id), new XAttribute("Version", packageReference.Version));
                    if (packageReference.IsDevelopmentDependency)
                    {
                        reference.Add(new XElement("PrivateAssets", "all"));
                    }

                    nugetReferences.Add(reference);
                }

                projectNode.Add(nugetReferences);
            }

            if (project.AssemblyReferences?.Count > 0)
            {
                var assemblyReferences = new XElement("ItemGroup");
                foreach (var assemblyReference in project.AssemblyReferences.Where(x => !IsDefaultIncludedAssemblyReference(x.Include)))
                {
                    assemblyReferences.Add(MakeAssemblyReference(assemblyReference));
                }

                projectNode.Add(assemblyReferences);
            }

            // manual includes
            if (project.IncludeItems?.Count > 0)
            {
                var includeGroup = new XElement("ItemGroup");
                foreach (var include in project.IncludeItems.Select(RemoveAllNamespaces))
                {
                    includeGroup.Add(include);
                }

                projectNode.Add(includeGroup);
            }

            return projectNode;
        }

        private static XElement MakeAssemblyReference(AssemblyReference assemblyReference)
        {
            var output = new XElement("Reference", new XAttribute("Include", assemblyReference.Include));

            if (assemblyReference.HintPath != null)
            {
                output.Add(new XElement("HintPath", assemblyReference.HintPath));
            }
            if (assemblyReference.Private != null)
            {
                output.Add(new XElement("Private", assemblyReference.Private));
            }
            if (assemblyReference.SpecificVersion != null)
            {
                output.Add(new XElement("SpecificVersion", assemblyReference.SpecificVersion));
            }
            if (assemblyReference.EmbedInteropTypes != null)
            {
                output.Add(new XElement("EmbedInteropTypes", assemblyReference.EmbedInteropTypes));
            }

            return output;
        }

        private static XElement RemoveAllNamespaces(XElement e)
        {
            return new XElement(e.Name.LocalName,
              (from n in e.Nodes()
               select ((n is XElement) ? RemoveAllNamespaces((XElement)n) : n)),
                  (e.HasAttributes) ?
                    (from a in e.Attributes()
                     where (!a.IsNamespaceDeclaration)
                     select new XAttribute(a.Name.LocalName, a.Value)) : null);
        }

        private bool IsDefaultIncludedAssemblyReference(string assemblyReference)
        {
            return new[]
            {
                "System",
                "System.Core",
                "System.Data",
                "System.Drawing",
                "System.IO.Compression.FileSystem",
                "System.Numerics",
                "System.Runtime.Serialization",
                "System.Xml",
                "System.Xml.Linq"
            }.Contains(assemblyReference);
        }

        private XElement GetMainPropertyGroup(Project project, FileInfo outputFile)
        {
            var mainPropertyGroup = new XElement("PropertyGroup");

            AddTargetFrameworks(mainPropertyGroup, project.TargetFrameworks);

			AddIfNotNull(mainPropertyGroup, "Configurations", string.Join(";", project.Configurations ?? Array.Empty<string>()));
			AddIfNotNull(mainPropertyGroup, "Optimize", project.Optimize ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "TreatWarningsAsErrors", project.TreatWarningsAsErrors ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "RootNamespace", project.RootNamespace != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.RootNamespace : null);
            AddIfNotNull(mainPropertyGroup, "AssemblyName", project.AssemblyName != Path.GetFileNameWithoutExtension(outputFile.Name) ? project.AssemblyName : null);
            AddIfNotNull(mainPropertyGroup, "AllowUnsafeBlocks", project.AllowUnsafeBlocks ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "SignAssembly", project.SignAssembly ? "true" : null);
            AddIfNotNull(mainPropertyGroup, "AssemblyOriginatorKeyFile", project.AssemblyOriginatorKeyFile);

            switch (project.Type)
            {
                case ApplicationType.ConsoleApplication:
                    mainPropertyGroup.Add(new XElement("OutputType", "Exe"));
                    break;
                case ApplicationType.WindowsApplication:
                    mainPropertyGroup.Add(new XElement("OutputType", "WinExe"));
                    break;
            }

            AddAssemblyAttributeNodes(mainPropertyGroup, project.AssemblyAttributes);
            AddPackageNodes(mainPropertyGroup, project.PackageConfiguration, project.AssemblyAttributes);

            return mainPropertyGroup;
        }

        private void AddPackageNodes(XElement mainPropertyGroup, PackageConfiguration packageConfiguration, AssemblyAttributes attributes)
        {
            if (packageConfiguration == null)
            {
                return;
            }

            AddIfNotNull(mainPropertyGroup, "Company", attributes?.Company);
            AddIfNotNull(mainPropertyGroup, "Authors", packageConfiguration.Authors);
            AddIfNotNull(mainPropertyGroup, "Copyright", packageConfiguration.Copyright);
            AddIfNotNull(mainPropertyGroup, "Description", packageConfiguration.Description);
            AddIfNotNull(mainPropertyGroup, "PackageIconUrl", packageConfiguration.IconUrl);
            AddIfNotNull(mainPropertyGroup, "PackageId", packageConfiguration.Id);
            AddIfNotNull(mainPropertyGroup, "PackageLicenseUrl", packageConfiguration.LicenseUrl);
            AddIfNotNull(mainPropertyGroup, "PackageProjectUrl", packageConfiguration.ProjectUrl);
            AddIfNotNull(mainPropertyGroup, "PackageReleaseNotes", packageConfiguration.ReleaseNotes);
            AddIfNotNull(mainPropertyGroup, "PackageTags", packageConfiguration.Tags);
            AddIfNotNull(mainPropertyGroup, "Version", packageConfiguration.Version);

            if(packageConfiguration.Id != null && packageConfiguration.Tags == null)
                mainPropertyGroup.Add(new XElement("PackageTags", "Library"));

            if (packageConfiguration.RequiresLicenseAcceptance)
            {
                mainPropertyGroup.Add(new XElement("PackageRequireLicenseAcceptance", "true"));
            }
        }

        private void AddAssemblyAttributeNodes(XElement mainPropertyGroup, AssemblyAttributes assemblyAttributes)
        {
            if (assemblyAttributes == null)
            {
                return;
            }

            var attributes = new[]
            {
                new KeyValuePair<string, string>("GenerateAssemblyTitleAttribute", assemblyAttributes.Title),
                new KeyValuePair<string, string>("GenerateAssemblyCompanyAttribute", assemblyAttributes.Company),
                new KeyValuePair<string, string>("GenerateAssemblyDescriptionAttribute", assemblyAttributes.Description),
                new KeyValuePair<string, string>("GenerateAssemblyProductAttribute", assemblyAttributes.Product),
                new KeyValuePair<string, string>("GenerateAssemblyCopyrightAttribute", assemblyAttributes.Copyright),
                new KeyValuePair<string, string>("GenerateAssemblyInformationalVersionAttribute", assemblyAttributes.InformationalVersion),
                new KeyValuePair<string, string>("GenerateAssemblyVersionAttribute", assemblyAttributes.Version),
                new KeyValuePair<string, string>("GenerateAssemblyFileVersionAttribute", assemblyAttributes.FileVersion),
                new KeyValuePair<string, string>("GenerateAssemblyConfigurationAttribute", assemblyAttributes.Configuration)
            };

            var childNodes = attributes
                .Where(x => x.Value != null)
                .Select(x => new XElement(x.Key, "false"))
                .ToArray();

            if (childNodes.Length == 0)
            {
                mainPropertyGroup.Add(new XElement("GenerateAssemblyInfo", "false"));
            }
            else
            {
                mainPropertyGroup.Add(childNodes);
            }
        }

        private void AddIfNotNull(XElement node, string elementName, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                node.Add(new XElement(elementName, value));
            }
        }

        private void AddTargetFrameworks(XElement mainPropertyGroup, IReadOnlyList<string> targetFrameworks)
        {
            if (targetFrameworks == null)
            {
                return;
            }
            else if (targetFrameworks.Count > 1)
            {
                AddIfNotNull(mainPropertyGroup, "TargetFrameworks", string.Join(";", targetFrameworks));
            }
            else
            {
                AddIfNotNull(mainPropertyGroup, "TargetFramework", targetFrameworks[0]);
            }
        }
    }
}
