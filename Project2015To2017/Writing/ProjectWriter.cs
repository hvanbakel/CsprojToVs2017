using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Project2015To2017.Writing
{
    internal sealed class ProjectWriter
    {
        public void Write(Project project, string outputFile)
        {
            var projectNode = new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"));

            projectNode.Add(GetMainPropertyGroup(project));

            if (project.ProjectReferences?.Count > 0)
            {
                var itemGroup = new XElement("ItemGroup");
                foreach (var projectReference in project.ProjectReferences)
                {
                    itemGroup.Add(new XElement("ProjectReference", new XAttribute("Include", projectReference)));
                }

                projectNode.Add(itemGroup);
            }

            if (project.PackageReferences?.Count > 0)
            {
                var nugetReferences = new XElement("ItemGroup");
                foreach (var packageReference in project.PackageReferences)
                {
                    nugetReferences.Add(new XElement("PackageReference", new XAttribute("Include", packageReference.Id), new XAttribute("Version", packageReference.Version)));
                }

                projectNode.Add(nugetReferences);
            }

            if (project.AssemblyReferences?.Count > 0)
            {
                var assemblyReferences = new XElement("ItemGroup");
                foreach (var assemblyReference in project.AssemblyReferences.Where(x => !IsDefaultIncludedAssemblyReference(x)))
                {
                    assemblyReferences.Add(new XElement("Reference", new XAttribute("Include", assemblyReference)));
                }

                projectNode.Add(assemblyReferences);
            }

            // resx wildcards
            projectNode.Add(
                new XElement("ItemGroup",
                    new XElement("EmbeddedResource", new XAttribute("Include", "**\\*.resx"))));

            // manual includes
            if (project.ItemsToInclude?.Count > 0)
            {
                projectNode.Add(new XElement("ItemGroup", project.ItemsToInclude.Select(RemoveAllNamespaces)));
            }

            using (var filestream = File.Open(outputFile, FileMode.Create))
            using (var streamWriter = new StreamWriter(filestream)) 
            {
                streamWriter.Write(projectNode.ToString());
            }
		}

		private static XElement RemoveAllNamespaces(XElement e)
		{
			return new XElement(e.Name.LocalName,
			  (from n in e.Nodes()
			   select ((n is XElement) ? RemoveAllNamespaces(n as XElement) : n)),
				  (e.HasAttributes) ?
					(from a in e.Attributes()
					 where (!a.IsNamespaceDeclaration)
					 select new XAttribute(a.Name.LocalName, a.Value)) : null);
		}

		private bool IsDefaultIncludedAssemblyReference(string assemblyReference)
        {
            return new string[]
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

        private XElement GetMainPropertyGroup(Project project)
        {
            var mainPropertyGroup = new XElement("PropertyGroup",
                ToTargetFrameworks(project.TargetFrameworks));

            AddIfNotNull(mainPropertyGroup, "Optimize", project.Optimize ? "true" : "false");
            AddIfNotNull(mainPropertyGroup, "TreatWarningsAsErrors", project.TreatWarningsAsErrors ? "true" : "false");
            AddIfNotNull(mainPropertyGroup, "RootNamespace", project.RootNamespace);
            AddIfNotNull(mainPropertyGroup, "RootNamespace", project.AssemblyName);
            AddIfNotNull(mainPropertyGroup, "AllowUnsafeBlocks", project.AllowUnsafeBlocks ? "true" : "false");
            AddIfNotNull(mainPropertyGroup, "DefineConstants", project.DefineConstants);

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
            AddPackageNodes(mainPropertyGroup, project.PackageConfiguration);

            return mainPropertyGroup;
        }

        private void AddPackageNodes(XElement mainPropertyGroup, PackageConfiguration packageConfiguration)
        {
            if (packageConfiguration== null)
            {
                return;
            }

            AddIfNotNull(mainPropertyGroup, "Authors", packageConfiguration.Authors);
            AddIfNotNull(mainPropertyGroup, "Copyright", packageConfiguration.Copyright);
            AddIfNotNull(mainPropertyGroup, "Description", packageConfiguration.Description);
            AddIfNotNull(mainPropertyGroup, "PackageIconUrl", packageConfiguration.IconUrl);
            AddIfNotNull(mainPropertyGroup, "PackageId", packageConfiguration.Id);
            AddIfNotNull(mainPropertyGroup, "PackageLicenseUrl", packageConfiguration.LicenseUrl);
            AddIfNotNull(mainPropertyGroup, "PackageProjectUrl", packageConfiguration.ProjectUrl);
            AddIfNotNull(mainPropertyGroup, "PackageReleaseNotes", packageConfiguration.ReleaseNotes);
            AddIfNotNull(mainPropertyGroup, "PackageTags", packageConfiguration.Tags);
            AddIfNotNull(mainPropertyGroup, "PackageVersion", packageConfiguration.Version);

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

            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyTitleAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyCompanyAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyDescriptionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyProductAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyCopyrightAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyInformationalVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyFileVersionAttribute", "false");
            AddIfNotNull(mainPropertyGroup, "GenerateAssemblyConfigurationAttribute", "false");
        }

        private void AddIfNotNull(XElement node, string elementName, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                node.Add(new XElement(elementName, value));
            }
        }

        private string ToOutputType(ApplicationType type)
        {
            

            return null;
        }

        private XElement ToTargetFrameworks(IReadOnlyList<string> targetFrameworks)
        {
            if (targetFrameworks.Count > 1)
            {
                return new XElement("TargetFrameworks", string.Join(";", targetFrameworks));
            }

            return new XElement("TargetFramework", targetFrameworks[0]);
        }
    }
}
