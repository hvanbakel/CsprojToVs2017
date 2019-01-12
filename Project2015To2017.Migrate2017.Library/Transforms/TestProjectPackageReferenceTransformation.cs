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

			if (definition.Type != ApplicationType.TestProject ||
			    existingPackageReferences.Any(x => x.Id == "Microsoft.NET.Test.Sdk")) return;

			var testReferences = new[]
			{
				new PackageReference {Id = "Microsoft.NET.Test.Sdk", Version = "15.9.0"},
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

			var adjustedPackageReferences = existingPackageReferences
				.Concat(testReferences)
				.ToArray();

			foreach (var reference in testReferences)
			{
				logger.LogInformation($"Adding NuGet reference to {reference.Id}, version {reference.Version}.");
			}

			definition.PackageReferences = adjustedPackageReferences;
		}
	}
}
