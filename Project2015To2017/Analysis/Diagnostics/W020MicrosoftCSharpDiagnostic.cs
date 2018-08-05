using Project2015To2017.Definition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project2015To2017.Analysis.Diagnostics
{
	internal class W020MicrosoftCSharpDiagnostic : DiagnosticBase
	{
		private static readonly uint DiagnoticId = 20;
		public static readonly string Code = DiagnoticId.ToDiagnosticCode();
		public override uint Id => DiagnoticId; 

		private static readonly string[] IncompatiblePrefixes = { "net1", "net2", "net3" };

		/// <param name="project"></param>
		/// <inheritdoc />
		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var reference = project.AssemblyReferences.FirstOrDefault(x => string.Equals(x.Include, "Microsoft.CSharp", StringComparison.OrdinalIgnoreCase));
			if (reference == null)
			{
				return System.Array.Empty<IDiagnosticResult>();
			}

			var net40Found = false;

			var list = new List<IDiagnosticResult>();
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

					list.Add(CreateDiagnosticResult($"'Microsoft.CSharp' assembly is incompatible with TargetFramework '{incompatiblePrefix}', version no less than 4.0 is expected.",
						reference.DefinitionElement, project.FilePath));
				}
			}

			if (!net40Found)
			{
				list.Add(CreateDiagnosticResult($"A better way to reference 'Microsoft.CSharp' assembly is using 'Microsoft.CSharp' NuGet package. It will simplify porting to other runtimes.",
					reference.DefinitionElement, project.FilePath));
			}

			return list;
		}
	}
}
