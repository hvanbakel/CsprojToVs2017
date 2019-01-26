using System;
using System.Collections.Immutable;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class UpgradeUseDefaultOutputPathTransformation : ITransformation
	{
		public void Transform(Project definition)
		{
			var docFileQueue = definition.PropertyGroups
				.ElementsAnyNamespace("DocumentationFile")
				.Where(x => HasDefaultLegacyOutputPath(x.Value));

			var removeQueue = definition.PropertyGroups
				.ElementsAnyNamespace("OutputPath")
				.Where(x => IsDefaultLegacyOutputPath(x.Value))
				.Union(docFileQueue)
				.ToImmutableArray();

			foreach (var element in removeQueue)
			{
				element.Remove();
			}

			bool IsDefaultLegacyOutputPath(string x) =>
				string.Equals(
					x.Replace('\\', '/'),
					@"bin/$(Configuration)/", StringComparison.OrdinalIgnoreCase);

			bool HasDefaultLegacyOutputPath(string x) =>
				x
					.Replace('\\', '/')
					.StartsWith(@"bin/$(Configuration)/", StringComparison.OrdinalIgnoreCase);
		}
	}
}