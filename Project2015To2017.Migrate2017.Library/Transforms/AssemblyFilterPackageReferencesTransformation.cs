using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017.Transforms
{
	public sealed class AssemblyFilterPackageReferencesTransformation : ILegacyOnlyProjectTransformation
	{
		public void Transform(Project definition)
		{
			var packageReferences =
				definition.PackageReferences ?? new List<PackageReference>();

			var packageIds = packageReferences
								.Select(x => x.Id)
								.ToList();

			var (assemblyReferences, removeQueue) =
					definition
						.AssemblyReferences
						//We don't need to keep any references to package files as these are
						//now generated dynamically at build time
						.Split(assemblyReference => !packageIds.Contains(assemblyReference.Include));

			foreach (var assemblyReference in removeQueue)
			{
				assemblyReference.DefinitionElement?.Remove();
			}

			definition.AssemblyReferences = assemblyReferences;
		}
	}
}
