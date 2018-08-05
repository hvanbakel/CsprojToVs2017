using System.Collections.Generic;

namespace Project2015To2017.Analysis
{
	public interface IReporter
	{
		/// <summary>
		/// Do the actual issue reporting
		/// </summary>
		/// <param name="results">Diagnostics to report</param>
		/// <param name="reporterOptions">Options for the reporter</param>
		void Report(IReadOnlyList<IDiagnosticResult> results, IReporterOptions reporterOptions);
	}
}