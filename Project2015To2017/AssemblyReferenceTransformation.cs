﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using System.Linq;

namespace Project2015To2017
{
    internal sealed class AssemblyReferenceTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";

            definition.AssemblyReferences = projectFile
                .Element(nsSys + "Project")
                ?.Elements(nsSys + "ItemGroup")
                .Elements(nsSys + "Reference")
                .Select(FormatAssemblyReference).ToList();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Remove Assembly References which are already in the Package References collection.
        /// </summary>
        /// <param name="projectDefinition">The project definition</param>
        public static void RemoveExtraAssemblyReferences(Project projectDefinition)
        {
            if (projectDefinition.AssemblyReferences?.Count > 0
                && projectDefinition.PackageReferences?.Count > 0)
            {
                var packageReferences =
                    projectDefinition.PackageReferences.AsQueryable();
                foreach (var assemblyReference in projectDefinition.AssemblyReferences
                    .ToArray())
                {
                    if (packageReferences.Any(x => x.Id == assemblyReference.Include))
                    {
                        projectDefinition.AssemblyReferences.Remove(assemblyReference);
                    }
                }
            }
        }

        private static AssemblyReference FormatAssemblyReference(XElement reference)
        {
            var output = new AssemblyReference
            {
                Include = reference.Attribute("Include")?.Value
            };

            var specificVersion = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == "SpecificVersion");
            if (specificVersion != null)
            {
                output.SpecificVersion = specificVersion.Value;
            }

            var hintPath = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == "HintPath");
            if (hintPath != null)
            {
                output.HintPath = hintPath.Value;
            }

            var isPrivate = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == "Private");
            if (isPrivate != null)
            {
                output.Private = isPrivate.Value;
            }

            var embedInteropTypes = reference.Descendants().FirstOrDefault(x => x.Name.LocalName == "EmbedInteropTypes");
            if (embedInteropTypes != null)
            {
                output.EmbedInteropTypes = embedInteropTypes.Value;
            }

            return output;
        }
    }
}
