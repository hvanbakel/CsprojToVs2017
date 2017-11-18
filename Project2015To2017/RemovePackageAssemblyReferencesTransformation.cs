using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017
{
	internal sealed class RemovePackageAssemblyReferencesTransformation : ITransformation
	{
		public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
		{
			if (definition.PackageReferences.Count == 0)
			{
				return Task.CompletedTask;
			}

			var packageReferenceIds = definition.PackageReferences.Select(x => x.Id).ToArray();
			definition.AssemblyReferences.RemoveAll(x => 
				x.HintPath != null && 
				packageReferenceIds.Any(p => x.HintPath.IndexOf(@"packages\" + p, StringComparison.OrdinalIgnoreCase) > 0));

			return Task.CompletedTask;
		}
	}
}
