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
			var packageConfig = PopulatePlaceHolders(
									definition.PackageConfiguration,
									definition.AssemblyAttributes
								);

			ConstrainPackageReferences(
				definition.PackageReferences, packageConfig
			);

			definition.PackageConfiguration = packageConfig;
		}

		private static void ConstrainPackageReferences(
				IReadOnlyList<PackageReference> rawPackageReferences,
				PackageConfiguration packageConfig
			)
		{
			var dependencies = packageConfig.Dependencies;
			if (dependencies == null || rawPackageReferences == null)
			{
				return;
			}

			var packageIdConstraints = dependencies.Select(dependency =>
			{
				var packageId = dependency.Attribute("id").Value;
				var constraint = dependency.Attribute("version").Value;

				return new { packageId, constraint };
			}).ToList();

			foreach (var packageReference in rawPackageReferences)
			{
				var matchingPackage = packageIdConstraints
										.SingleOrDefault(dependency =>
											packageReference.Id.Equals(dependency.packageId, StringComparison.OrdinalIgnoreCase)
										);

				if (matchingPackage != null)
				{
					packageReference.Version = matchingPackage.constraint;
				}
			}
		}

		private PackageConfiguration PopulatePlaceHolders(PackageConfiguration rawPackageConfig, AssemblyAttributes assemblyAttributes)
		{
			return new PackageConfiguration
			{
				Id = PopulatePlaceHolder("id", rawPackageConfig.Id, assemblyAttributes.AssemblyName),
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
				Dependencies = rawPackageConfig.Dependencies
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
