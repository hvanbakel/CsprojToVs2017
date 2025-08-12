using System.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class TestProjectPackageReferenceTransformation : ILegacyOnlyProjectTransformation
	{
		private readonly ILogger logger;

		public TestProjectPackageReferenceTransformation(ILogger logger = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
		}

		public void Transform(Project definition)
		{
			var existingPackageReferences = definition.PackageReferences;

			if (definition.Type != ApplicationType.TestProject) return;

			var testReferences = new[]
			{
				new PackageReference {Id = "Microsoft.NET.Test.Sdk", Version = "16.0.1"},
				new PackageReference {Id = "MSTest.TestAdapter", Version = "1.4.0"},
				new PackageReference {Id = "MSTest.TestFramework", Version = "1.4.0"}
			};

			var versions = definition.TargetFrameworks?
				.Select(f => int.TryParse(f.Replace("net", string.Empty), out int result) ? result : default(int?))
				.Where(x => x.HasValue)
				.Select(v => v < 100 ? v * 10 : v);

			if (versions != null)
			{
				if (versions.Any(v => v < 450))
				{
					logger.LogWarning("Target framework net40 is not compatible with the MSTest NuGet packages. Please consider updating the target framework of your test project(s)");
				}
			}

			// Check if any test packages already exist
			var hasTestSdk = existingPackageReferences.Any(x => x.Id == "Microsoft.NET.Test.Sdk");
			var hasMSTestAdapter = existingPackageReferences.Any(x => x.Id == "MSTest.TestAdapter");
			var hasMSTestFramework = existingPackageReferences.Any(x => x.Id == "MSTest.TestFramework");

			// If Microsoft.NET.Test.Sdk already exists, don't add anything (maintain backward compatibility)
			if (hasTestSdk) return;

			// Only add packages that don't already exist
			var packagesToAdd = testReferences
				.Where(testRef => !existingPackageReferences.Any(existingRef => existingRef.Id == testRef.Id))
				.ToArray();

			var adjustedPackageReferences = existingPackageReferences
				.Concat(packagesToAdd)
				.ToArray();

			foreach (var reference in packagesToAdd)
			{
				logger.LogInformation($"Adding NuGet reference to {reference.Id}, version {reference.Version}.");
			}

			definition.PackageReferences = adjustedPackageReferences;
		}
	}
}
