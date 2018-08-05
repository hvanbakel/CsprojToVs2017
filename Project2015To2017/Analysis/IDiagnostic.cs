using System.Collections.Generic;
using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	public interface IDiagnostic
	{
		bool SkipForLegacyProject { get; }
		bool SkipForModernProject { get; }

		IReadOnlyList<IDiagnosticResult> Analyze(Project project);
	}
}