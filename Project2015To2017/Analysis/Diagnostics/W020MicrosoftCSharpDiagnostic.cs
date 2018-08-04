using Project2015To2017.Definition;
using System;
using System.Linq;

namespace Project2015To2017.Analysis.Diagnostics
{
	internal class W020MicrosoftCSharpDiagnostic : DiagnosticBase
	{
		/// <inheritdoc />
		public W020MicrosoftCSharpDiagnostic() : base(20)
		{
		}

		private static readonly string[] IncompatiblePrefixes = { "net1", "net2", "net3" };

		/// <param name="project"></param>
		/// <inheritdoc />
		protected override void AnalyzeImpl(Project project)
		{
			var reference = project.AssemblyReferences.FirstOrDefault(x => string.Equals(x.Include, "Microsoft.CSharp", StringComparison.OrdinalIgnoreCase));
			if (reference == null)
			{
				return;
			}

			var net40Found = false;

			foreach (var framework in project.TargetFrameworks.Where(x => x.StartsWith("net", StringComparison.OrdinalIgnoreCase)))
			{
				if (framework.StartsWith("net40"))
				{
					net40Found = true;
					continue;
				}

				foreach (var incompatiblePrefix in IncompatiblePrefixes)
				{
					if (!framework.StartsWith(incompatiblePrefix))
					{
						continue;
					}

					Report($"'Microsoft.CSharp' assembly is incompatible with TargetFramework '{incompatiblePrefix}', version no less than 4.0 is expected.",
						reference.DefinitionElement, project.FilePath);
				}
			}

			if (!net40Found)
				Report($"A better way to reference 'Microsoft.CSharp' assembly is using 'Microsoft.CSharp' NuGet package. It will simplify porting to other runtimes.",
					reference.DefinitionElement, project.FilePath);
		}
	}
}
