﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;
using System.Linq;

namespace Project2015To2017
{
    internal sealed class FileTransformation : ITransformation
    {
        private static readonly IReadOnlyList<string> ItemsToProject = new[]
        {
            "None",
            "Content",
            "AdditionalFiles",
            "CodeAnalysisDictionary",
            "ApplicationDefinition",
            "Page",
            "Resource",
            "SplashScreen",
            "DesignData",
            "DesignDataWithDesignTimeCreatableTypes",
            "EntityDeploy",
            "XamlAppDef"
        };

        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
            var itemGroups = projectFile
                .Element(nsSys + "Project")
                .Elements(nsSys + "ItemGroup");

            var compileManualIncludes = FindNonWildcardMatchedFiles(projectFolder, itemGroups, "*.cs", nsSys + "Compile");
            var otherIncludes = ItemsToProject.SelectMany(x => itemGroups.Elements(nsSys + x));

            // Remove packages.config since those references were already added to the CSProj file.
            otherIncludes.Where(x => x.Attribute("Include")?.Value == "packages.config").Remove();

            definition.ItemsToInclude = compileManualIncludes.Concat(otherIncludes).ToArray();

            return Task.CompletedTask;
        }

        private static IReadOnlyList<XElement> FindNonWildcardMatchedFiles(
            DirectoryInfo projectFolder,
            IEnumerable<XElement> itemGroups,
            string wildcard,
            XName elementName)
        {
            var manualIncludes = new List<XElement>();
            var filesMatchingWildcard = new List<string>();
            foreach (var compiledFile in itemGroups.Elements(elementName))
            {
                var includeAttribute = compiledFile.Attribute("Include");
                if (includeAttribute != null && !includeAttribute.Value.Contains("*"))
                {
                    if (!Path.GetFullPath(Path.Combine(projectFolder.FullName, includeAttribute.Value)).StartsWith(projectFolder.FullName))
                    {
                        Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile}.");
                        manualIncludes.Add(compiledFile);
                    }
                    else if (compiledFile.Attributes().Count() != 1)
                    {
                        Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile}.");
                        manualIncludes.Add(compiledFile);
                    }
                    else if (compiledFile.Elements().Count() != 0)
                    {
                        var dependentUpon = compiledFile.Element(elementName.Namespace + "DependentUpon");
                        if (dependentUpon != null && dependentUpon.Value.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
                        {
                            // resx generated code file
                            manualIncludes.Add(new XElement(
                                "Compile",
                                new XAttribute("Update", includeAttribute.Value),
                                new XElement("DependentUpon", dependentUpon.Value)));

                            filesMatchingWildcard.Add(includeAttribute.Value);
                        }
                        else
                        {
                            Console.WriteLine($"Include cannot be done through wildcard, adding as separate include {compiledFile}.");
                            manualIncludes.Add(compiledFile);
                        }
                    }
                    else
                    {
                        filesMatchingWildcard.Add(includeAttribute.Value);
                    }
                }
                else
                {
                    Console.WriteLine($"Compile found with no or wildcard include, full node {compiledFile}.");
                }
            }

            var filesInFolder = projectFolder.EnumerateFiles(wildcard, SearchOption.AllDirectories).Select(x => x.FullName).ToArray();
            var knownFullPaths = manualIncludes
                .Select(x => x.Attribute("Include")?.Value)
                .Where(x => x != null)
                .Concat(filesMatchingWildcard)
                .Select(x => Path.GetFullPath(Path.Combine(projectFolder.FullName, x)))
                .ToArray();

            foreach (var nonListedFile in filesInFolder.Except(knownFullPaths))
            {
                if (nonListedFile.StartsWith(Path.Combine(projectFolder.FullName + "\\obj"), StringComparison.OrdinalIgnoreCase))
                {
                    // skip the generated files in obj
                    continue;
                }

                Console.WriteLine($"File found which was not included, consider removing {nonListedFile}.");
            }

            foreach (var fileNotOnDisk in knownFullPaths.Except(filesInFolder).Where(x => x.StartsWith(projectFolder.FullName, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"File was included but is not on disk: {fileNotOnDisk}.");
            }

            return manualIncludes;
        }
    }
}
