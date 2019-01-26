using System;
using System.Collections.Generic;
using Project2015To2017.Analysis;
using Project2015To2017.Definition;

namespace Project2015To2017.Migrate2017.Diagnostics
{
	public sealed class W030LegacyDebugTypesDiagnostic : DiagnosticBase
	{
		public W030LegacyDebugTypesDiagnostic() : base(30)
		{
		}

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();

			foreach (var x in project.ProjectDocument.Descendants(project.XmlNamespace + "DebugType"))
			{
				if (x.Value.Equals("portable", StringComparison.OrdinalIgnoreCase))
					continue;

				list.Add(
					CreateDiagnosticResult(project,
							$"Consider migrating to 'portable' debug type, cross-platform alternative to legacy Windows PDBs.",
							project.FilePath)
						.LoadLocationFromElement(x));
			}

			return list;
		}
	}
}