using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal sealed class NugetPackageTransformation : ITransformation
	{
		public void Transform(Project definition, IProgress<string> progress)
		{
			if (definition.PackageConfiguration == null)
			{
				return;
			}
			
			var packageConfig = PopulatePlaceHolders(definition.PackageConfiguration, definition);

			ConstrainPackageReferences(definition.PackageReferences, packageConfig);

			definition.PackageConfiguration = packageConfig;
		}

		private static void ConstrainPackageReferences(IReadOnlyList<PackageReference> rawPackageReferences, PackageConfiguration packageConfig)
		{
			var dependencies = packageConfig.Dependencies;
			if (dependencies == null || rawPackageReferences == null)
			{
				return;
			}

			var packageIdConstraints = dependencies.Select(dependency =>
			{
				return new
				{
					PackageId = dependency.Attribute("id").Value,
					Version = dependency.Attribute("version").Value
				};
			}).ToArray();

			foreach (var packageReference in rawPackageReferences)
			{
				var matchingPackage = packageIdConstraints.SingleOrDefault(dependency => packageReference.Id.Equals(dependency.PackageId, StringComparison.OrdinalIgnoreCase));

				if (matchingPackage != null)
				{
					packageReference.Version = matchingPackage.Version;
				}
			}
		}

		private PackageConfiguration PopulatePlaceHolders(PackageConfiguration rawPackageConfig, Project project)
		{
			var assemblyAttributes = project.AssemblyAttributes;

			return new PackageConfiguration
			{
				//Id does not need to be specified in new project format if it is just the same as the assembly name
				Id = rawPackageConfig.Id == "$id$" ? null : rawPackageConfig.Id,
				Version = PopulatePlaceHolder("version", rawPackageConfig.Version, assemblyAttributes.InformationalVersion ?? assemblyAttributes.Version),
				Authors = PopulatePlaceHolder("author", rawPackageConfig.Authors, assemblyAttributes.Company),
				Description = PopulatePlaceHolder("description", rawPackageConfig.Description, assemblyAttributes.Description),
				Copyright = PopulatePlaceHolder("copyright", rawPackageConfig.Copyright, assemblyAttributes.Copyright),
				LicenseUrl = rawPackageConfig.LicenseUrl,
				ProjectUrl = rawPackageConfig.ProjectUrl,
				IconUrl = rawPackageConfig.IconUrl,
				Tags = rawPackageConfig.Tags,
				ReleaseNotes = rawPackageConfig.ReleaseNotes,
				RequiresLicenseAcceptance = rawPackageConfig.RequiresLicenseAcceptance,
				Dependencies = rawPackageConfig.Dependencies,
				NuspecFile = rawPackageConfig.NuspecFile
			};
		}

		private string PopulatePlaceHolder(string placeHolder, string nuSpecValue, string assemblyAttributeValue)
		{
			if (nuSpecValue == $"${placeHolder}$")
			{
				return assemblyAttributeValue ?? nuSpecValue;
			}
			else
			{
				return nuSpecValue;
			}
		}
	}
}
