using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.Logging;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class UpgradeFrameworkAssembliesToNuGetTransformation : IModernOnlyProjectTransformation
	{
		private readonly ILogger logger;

		public UpgradeFrameworkAssembliesToNuGetTransformation(ILogger logger = null)
		{
			this.logger = logger ?? NoopLogger.Instance;
		}

		public void Transform(Project definition)
		{
			var references = SystemNuGetPackages.DetectUpgradeableReferences(definition);
			foreach (var (_, _, assemblyReference) in references)
			{
				assemblyReference.DefinitionElement?.Remove();
			}

			definition.AssemblyReferences = definition.AssemblyReferences
				.Except(references.Select(x => x.reference))
				.ToImmutableArray();

			var packageReferences = references
				.Select(x => new PackageReference { Id = x.name, Version = x.version })
				.ToImmutableArray();

			var adjustedPackageReferences = definition.PackageReferences
				.Concat(packageReferences)
				.ToArray();

			foreach (var reference in packageReferences)
			{
				logger.LogDebug($"Adding NuGet reference to {reference.Id}, version {reference.Version}.");
			}

			definition.PackageReferences = adjustedPackageReferences;
		}
	}
}
