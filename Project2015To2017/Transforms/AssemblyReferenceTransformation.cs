using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Transforms
{
	internal sealed class AssemblyReferenceTransformation : ITransformation
	{
		public Project Transform(Project definition, IProgress<string> progress)
		{
			var packageReferences =
				definition.PackageReferences ?? new List<PackageReference>();

			var packageIds = packageReferences
								.Select(x => x.Id)
								.ToList();

			var assemblyReferences =
					definition
						.AssemblyReferences
						//This reference is obsolete.
						?.Where(x => !x.Include.Equals("Microsoft.CSharp", StringComparison.OrdinalIgnoreCase))
						//We don't need to keep any references to package files as these are
						//now generated dynamically at build time
						.Where(assemblyReference => !packageIds.Contains(assemblyReference.Include));

			var transformedDefinition = definition
											.WithAssemblyReferences(assemblyReferences);

			return transformedDefinition;
		}
	}
}
