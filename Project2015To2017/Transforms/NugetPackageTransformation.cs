using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal sealed class NugetPackageTransformation : ITransformation
	{
		public Project Transform(Project definition, IProgress<string> progress)
		{
			var packageConfig = PopulatePlaceHolders(
									definition.PackageConfiguration,
									definition.AssemblyAttributes
								);

			var packageReferences = ConstrainedPackageReferences(
										definition.PackageReferences, packageConfig
									);

			var adjustedProject = definition
										.WithPackageConfig(packageConfig)
										.WithPackageReferences(packageReferences);

			return adjustedProject;
		}

		private static IReadOnlyList<PackageReference> ConstrainedPackageReferences(
				IReadOnlyList<PackageReference> rawPackageReferences,
				PackageConfiguration packageConfig
			)
		{
			var dependencies = packageConfig.Dependencies;
			if (dependencies == null || rawPackageReferences == null)
			{
				return rawPackageReferences;
			}

			var packageIdConstraints = dependencies.Select(dependency =>
			{
				var packageId = dependency.Attribute("id").Value;
				var constraint = dependency.Attribute("version").Value;

				return new {packageId, constraint};
			}).ToList();

			var adjustedPackageReferences = rawPackageReferences.Select(
							packageReference =>
							{
								var matchingPackage = packageIdConstraints
														.SingleOrDefault(dependency =>
															packageReference.Id.Equals(dependency.packageId, StringComparison.OrdinalIgnoreCase)
														);

								if (matchingPackage == null)
								{
									return packageReference;
								}

								return packageReference.WithVersion(matchingPackage.constraint);
							}
						).ToList()
						.AsReadOnly();

			return adjustedPackageReferences;
		}

		private PackageConfiguration PopulatePlaceHolders(PackageConfiguration rawPackageConfig, AssemblyAttributes assemblyAttributes)
		{
			return new PackageConfiguration(
					id: PopulatePlaceHolder("id", rawPackageConfig.Id, assemblyAttributes.AssemblyName),
					version: PopulatePlaceHolder("version", rawPackageConfig.Version, assemblyAttributes.InformationalVersion ?? assemblyAttributes.Version),
					authors: PopulatePlaceHolder("author", rawPackageConfig.Authors, assemblyAttributes.Company),
					description: PopulatePlaceHolder("description", rawPackageConfig.Description, assemblyAttributes.Description),
					copyright: PopulatePlaceHolder("copyright", rawPackageConfig.Copyright, assemblyAttributes.Copyright),
					licenseUrl: rawPackageConfig.LicenseUrl,
					projectUrl: rawPackageConfig.ProjectUrl,
					iconUrl: rawPackageConfig.IconUrl,
					tags: rawPackageConfig.Tags,
					releaseNotes: rawPackageConfig.ReleaseNotes,
					requiresLicenseAcceptance: rawPackageConfig.RequiresLicenseAcceptance,
					dependencies: rawPackageConfig.Dependencies
				);
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
