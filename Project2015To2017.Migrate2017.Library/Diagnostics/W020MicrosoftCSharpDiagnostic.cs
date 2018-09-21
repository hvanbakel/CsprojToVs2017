using System;
using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;

namespace Project2015To2017.Migrate2017.Diagnostics
{
	public sealed class W020MicrosoftCSharpDiagnostic : DiagnosticBase
	{
		private static readonly string[] IncompatiblePrefixes = { "net1", "net2", "net3" };

		/// <inheritdoc />
		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var reference = project.AssemblyReferences.FirstOrDefault(x => string.Equals(x.Include, "Microsoft.CSharp", StringComparison.OrdinalIgnoreCase));
			if (reference == null)
			{
				return Array.Empty<IDiagnosticResult>();
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

					list.Add(
						CreateDiagnosticResult(project,
								$"'Microsoft.CSharp' assembly is incompatible with TargetFramework '{incompatiblePrefix}', version no less than 4.0 is expected.",
								project.FilePath)
							.LoadLocationFromElement(reference.DefinitionElement));
				}
			}

			if (!net40Found)
			{
				list.Add(
					CreateDiagnosticResult(project,
							"A better way to reference 'Microsoft.CSharp' assembly is using 'Microsoft.CSharp' NuGet package. It will simplify porting to other runtimes.",
							project.FilePath)
						.LoadLocationFromElement(reference.DefinitionElement));
			}

			return list;
		}

		public W020MicrosoftCSharpDiagnostic() : base(20)
		{
		}
	}
}