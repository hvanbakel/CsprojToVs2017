using System.Collections.Generic;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis.Diagnostics
{
	public sealed class W032OldLanguageVersionDiagnostic : DiagnosticBase
	{
		public W032OldLanguageVersionDiagnostic() : base(32)
		{
		}

		public override IReadOnlyList<IDiagnosticResult> Analyze(Project project)
		{
			var list = new List<IDiagnosticResult>();

			foreach (var x in project.ProjectDocument.Descendants(project.XmlNamespace + "LangVersion"))
			{
				// last 2 versions + default
				var version = x.Value;
				if (version.Equals("7.2", ExtensionMethods.BestAvailableStringIgnoreCaseComparison)) continue;
				if (version.Equals("7.3", ExtensionMethods.BestAvailableStringIgnoreCaseComparison)) continue;
				if (version.Equals("latest", ExtensionMethods.BestAvailableStringIgnoreCaseComparison)) continue;
				if (version.Equals("default", ExtensionMethods.BestAvailableStringIgnoreCaseComparison)) continue;

				list.Add(
					CreateDiagnosticResult(project,
							$"Consider upgrading language version to the latest ({version}).",
							project.FilePath)
						.LoadLocationFromElement(x));
			}

			return list;
		}
	}
}