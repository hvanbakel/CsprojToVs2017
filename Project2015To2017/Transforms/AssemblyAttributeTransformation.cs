using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public class AssemblyAttributeTransformation : ITransformation
	{
		public AssemblyAttributeTransformation() : this(false)
		{
		}
		public AssemblyAttributeTransformation(bool keepAssemblyInfoFile)
		{
			KeepAssemblyInfoFile = keepAssemblyInfoFile;
		}

		public bool KeepAssemblyInfoFile { get; }

		public void Transform(Project definition, IProgress<string> progress)
		{
			if (definition.AssemblyAttributes == null)
			{
				if (definition.HasMultipleAssemblyInfoFiles)
					definition.AssemblyAttributeProperties = new[] { new XElement("GenerateAssemblyInfo", "false") };
				return;
			}

			if (!KeepAssemblyInfoFile)
			{
				definition.AssemblyAttributeProperties = definition.AssemblyAttributeProperties
					.Concat(AssemblyAttributeNodes(definition.AssemblyAttributes, definition.PackageConfiguration, progress))
					.ToArray();
			}
			else
			{
				progress.Report("Keep AssemblyInfo");
				definition.AssemblyAttributeProperties = new[] { new XElement("GenerateAssemblyInfo", "false") };
			}

			if (definition.AssemblyAttributes.File != null && PointlessAssemblyInfo(definition.AssemblyAttributes))
			{
				definition.Deletions = definition
					.Deletions
					.Concat(new[] { definition.AssemblyAttributes.File })
					.ToArray();

				if (AssemblyInfoFolderJustAssemblyInfo(definition.AssemblyAttributes))
				{
					definition.Deletions = definition
						.Deletions
						.Concat(new[] { definition.AssemblyAttributes.File.Directory })
						.ToArray();
				}
			}
		}

		private bool AssemblyInfoFolderJustAssemblyInfo(AssemblyAttributes assemblyAttributes)
		{
			//Look if only the assembly info file is in the directory
			return assemblyAttributes.File.Directory.EnumerateFileSystemInfos().Count() <= 1;
		}

		private bool PointlessAssemblyInfo(AssemblyAttributes assemblyAttributes)
		{
			var file = assemblyAttributes.FileContents;

			return !file.Members.Any() && !file.AttributeLists.Any();
		}

		private static IReadOnlyList<XElement> AssemblyAttributeNodes(AssemblyAttributes assemblyAttributes, PackageConfiguration packageConfig, IProgress<string> progress)
		{
			progress.Report("Moving attributes from AssemblyInfo to project file");

			var versioningProperties = VersioningProperties(assemblyAttributes, packageConfig, progress);
			var otherProperties = OtherProperties(assemblyAttributes, packageConfig, progress);

			var childNodes = otherProperties.Concat(versioningProperties).ToArray();

			if (childNodes.Length == 0)
			{
				//Assume that the assembly info is coming from another file
				//which we don't have sight of so leave it up to consumer to
				//convert over if they wish
				return new[] { new XElement("GenerateAssemblyInfo", "false") };
			}

			return childNodes;
		}

		private static IReadOnlyList<XElement> OtherProperties(AssemblyAttributes assemblyAttributes,
			PackageConfiguration packageConfig, IProgress<string> progress)
		{
			var toReturn = new[]
			{
				CreateElementIfNotNull(assemblyAttributes.Title, "AssemblyTitle"),
				CreateElementIfNotNull(assemblyAttributes.Company, "Company"),
				CreateElementIfNotNull(assemblyAttributes.Product, "Product"),

				//And a couple of properties which can be superceded by the package config
				CreateElementIfNotNull(assemblyAttributes.Description, packageConfig?.Description, "Description", progress),
				CreateElementIfNotNull(assemblyAttributes.Copyright, packageConfig?.Copyright, "Copyright", progress),

				assemblyAttributes.Configuration != null
					?
					//If it is included, chances are that the developer has used
					//preprocessor flags which we can't yet process
					//so just leave it in AssemblyInfo file
					new XElement("GenerateAssemblyConfigurationAttribute", false)
					: null
			}.Where(x => x != null).ToArray();

			assemblyAttributes.Title = null;
			assemblyAttributes.Company = null;
			assemblyAttributes.Description = null;
			assemblyAttributes.Product = null;
			assemblyAttributes.Copyright = null;

			return toReturn;
		}

		private static XElement CreateElementIfNotNull(string assemblyInfoValue, string packageConfigValue, string description, IProgress<string> progress)
		{
			if (packageConfigValue != null && packageConfigValue != assemblyInfoValue)
			{
				if (assemblyInfoValue != null)
				{
					progress.Report(
						$"Taking nuspec {description} property value {packageConfigValue} " +
						$"over AssemblyInfo value {assemblyInfoValue}");
				}

				return CreateElementIfNotNull(packageConfigValue, description);
			}
			else
			{
				return assemblyInfoValue == null ? null : CreateElementIfNotNull(assemblyInfoValue, description);
			}
		}

		private static IReadOnlyList<XElement> VersioningProperties(AssemblyAttributes assemblyAttributes,
			PackageConfiguration packageConfig, IProgress<string> progress)
		{
			var toReturn = new[]
			{
				CreateElementIfNotNull(assemblyAttributes.InformationalVersion, packageConfig?.Version, "Version", progress),
				CreateElementIfNotNull(assemblyAttributes.Version, "AssemblyVersion"),

				//The AssemblyInfo behaviour was to fallback on the AssemblyVersion for the file version
				//but in the new format, this doesn't happen so we explicitly copy the value across
				CreateElementIfNotNull(assemblyAttributes.FileVersion, "FileVersion") ?? CreateElementIfNotNull(assemblyAttributes.Version, "FileVersion")
			}.Where(x => x != null).ToArray();

			assemblyAttributes.InformationalVersion = null;
			assemblyAttributes.Version = null;
			assemblyAttributes.FileVersion = null;

			return toReturn;
		}

		private static XElement CreateElementIfNotNull(string attribute, string name)
		{
			return attribute != null ? new XElement(name, attribute) : null;
		}
	}
}