using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	public sealed class AssemblyAttributeTransformation
		: ITransformationWithTargetMoment, ILegacyOnlyProjectTransformation
	{
		private readonly ILogger logger;

		public AssemblyAttributeTransformation(ILogger logger, bool keepAssemblyInfoFile = false)
		{
			this.logger = logger;
			this.KeepAssemblyInfoFile = keepAssemblyInfoFile;
		}

		public bool KeepAssemblyInfoFile { get; }

		public void Transform(Project definition)
		{
			if (definition.AssemblyAttributes == null)
			{
				if (definition.HasMultipleAssemblyInfoFiles)
				{
					definition.SetProperty("GenerateAssemblyInfo", "false");
				}

				return;
			}

			if (!this.KeepAssemblyInfoFile)
			{
				definition.AssemblyAttributeProperties = definition.AssemblyAttributeProperties
					.Concat(AssemblyAttributeNodes(definition.AssemblyAttributes, definition.PackageConfiguration, logger))
					.ToArray();
			}
			else
			{
				logger.LogInformation("Keep AssemblyInfo");
				definition.AssemblyAttributeProperties = new[] { new XElement("GenerateAssemblyInfo", "false") };
			}

			//Add to the primary property group, which then gives scope for other generic transforms to process it
			definition.PrimaryPropertyGroup().Add(definition.AssemblyAttributeProperties);

			MarkForDeletion(definition);
		}

		private void MarkForDeletion(Project definition)
		{
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

		private static IReadOnlyList<XElement> AssemblyAttributeNodes(AssemblyAttributes assemblyAttributes, PackageConfiguration packageConfig, ILogger logger)
		{
			logger.LogDebug("Moving attributes from AssemblyInfo to project file");

			var versioningProperties = VersioningProperties(assemblyAttributes, packageConfig, logger);
			var signingProperties = SigningProperties(assemblyAttributes, packageConfig, logger);
			var otherProperties = OtherProperties(assemblyAttributes, packageConfig, logger);

			var childNodes = otherProperties.Concat(signingProperties).Concat(versioningProperties).ToArray();

			if (childNodes.Length == 0)
			{
				//Assume that the assembly info is coming from another file
				//which we don't have sight of so leave it up to consumer to
				//convert over if they wish
				return new[] { new XElement("GenerateAssemblyInfo", "false") };
			}

			return childNodes;
		}

		private static IReadOnlyList<XElement> OtherProperties(AssemblyAttributes assemblyAttributes, PackageConfiguration packageConfig, ILogger logger)
		{
			var configCanBeStripped = string.IsNullOrEmpty(assemblyAttributes.Configuration);

			var toReturn = new[]
			{
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Title, "AssemblyTitle"),
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Company, "Company"),
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Product, "Product"),
				CreateElementIfNotNullOrEmpty(assemblyAttributes.NeutralLanguage, "NeutralLanguage"),


				//And a couple of properties which can be superceded by the package config
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Description, packageConfig?.Description, "Description", logger),
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Copyright, packageConfig?.Copyright, "Copyright", logger),

				!configCanBeStripped
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
			assemblyAttributes.NeutralLanguage = null;

			if (assemblyAttributes.Culture == string.Empty)
			{
				assemblyAttributes.Culture = null;
			}

			if (assemblyAttributes.Trademark == string.Empty)
			{
				assemblyAttributes.Trademark = null;
			}

			if (configCanBeStripped)
			{
				assemblyAttributes.Configuration = null;
			}

			return toReturn;
		}

		private static XElement CreateElementIfNotNullOrEmpty(string assemblyInfoValue, string packageConfigValue, string description, ILogger logger)
		{
			if (packageConfigValue != null && packageConfigValue != assemblyInfoValue)
			{
				if (assemblyInfoValue != null)
				{
					logger.LogWarning(
						$"Taking nuspec {description} property value {packageConfigValue} " +
						$"over AssemblyInfo value {assemblyInfoValue}");
				}

				return CreateElementIfNotNullOrEmpty(packageConfigValue, description);
			}
			else
			{
				return CreateElementIfNotNullOrEmpty(assemblyInfoValue, description);
			}
		}

		private static IReadOnlyList<XElement> VersioningProperties(AssemblyAttributes assemblyAttributes,
			PackageConfiguration packageConfig, ILogger logger)
		{
			var toReturn = new[]
			{
				assemblyAttributes.IsNonDeterministic ? new XElement("Deterministic", false) : null,
				CreateElementIfNotNullOrEmpty(assemblyAttributes.InformationalVersion, packageConfig?.Version, "Version", logger),
				CreateElementIfNotNullOrEmpty(assemblyAttributes.Version, "AssemblyVersion"),

				//The AssemblyInfo behaviour was to fallback on the AssemblyVersion for the file version
				//but in the new format, this doesn't happen so we explicitly copy the value across
				CreateElementIfNotNullOrEmpty(assemblyAttributes.FileVersion, "FileVersion") ?? CreateElementIfNotNullOrEmpty(assemblyAttributes.Version, "FileVersion")
			}.Where(x => x != null).ToArray();

			assemblyAttributes.InformationalVersion = null;
			assemblyAttributes.Version = null;
			assemblyAttributes.FileVersion = null;

			return toReturn;
		}

		private static IReadOnlyList<XElement> SigningProperties(AssemblyAttributes assemblyAttributes,
			PackageConfiguration packageConfig, ILogger logger)
		{
			var toReturn = new[]
			{
				assemblyAttributes.IsSigned ? new XElement("SignAssembly", true) : null,
				assemblyAttributes.KeyFile != null ? CreateElementIfNotNullOrEmpty(assemblyAttributes.DelaySign, "DelaySign") : null,
				CreateElementIfNotNullOrEmpty(assemblyAttributes.KeyFile, "AssemblyOriginatorKeyFile")
			}.Where(x => x != null).ToArray();

			assemblyAttributes.DelaySign = null;
			assemblyAttributes.KeyFile = null;

			return toReturn;
		}

		private static XElement CreateElementIfNotNullOrEmpty(string attribute, string name)
		{
			return !string.IsNullOrEmpty(attribute) ? new XElement(name, attribute) : null;
		}

		public TargetTransformationExecutionMoment ExecutionMoment =>
			TargetTransformationExecutionMoment.Early;
	}
}