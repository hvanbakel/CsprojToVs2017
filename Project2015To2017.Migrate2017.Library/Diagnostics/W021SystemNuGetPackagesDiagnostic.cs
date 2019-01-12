using System.Collections.Generic;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;

namespace Project2015To2017.Migrate2017.Diagnostics
{
	public sealed class W021SystemNuGetPackagesDiagnostic : DiagnosticBase
	{
		public W021SystemNuGetPackagesDiagnostic() : base(21)
		{
		}

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();

			foreach (var (name, _, reference) in SystemNuGetPackages.DetectUpgradeableReferences(project))
			{
				list.Add(
					CreateDiagnosticResult(project,
							$"A better way to reference '{name}' assembly is using respective '{name}' NuGet package. It will simplify porting to other runtimes and enable possible .NET SDK tooling improvements.",
							project.FilePath)
						.LoadLocationFromElement(reference.DefinitionElement));
			}

			return list;
		}
	}
}