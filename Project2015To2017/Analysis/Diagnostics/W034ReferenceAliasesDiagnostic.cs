using System.Collections.Generic;
using System.Linq;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W034ReferenceAliasesDiagnostic : DiagnosticBase
	{
		public W034ReferenceAliasesDiagnostic() : base(34)
		{
		}

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();

			foreach (var reference in project.ProjectReferences.Where(x => !string.IsNullOrEmpty(x.Aliases)))
			{
				list.Add(
					CreateDiagnosticResult(project,
						$"ProjectReference ['{reference.Include}'] aliases are a feature of low support. Consider dropping their usage.",
						project.FilePath));
			}

			return list;
		}
	}
}