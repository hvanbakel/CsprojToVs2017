using System.Collections.Generic;
using Project2015To2017.Transforms;

namespace Project2015To2017.Analysis
{
	public abstract class ReporterBase : IReporter
	{
		/// <inheritdoc />
		protected abstract void Report(string code, string message, string source, uint sourceLine);

		/// <inheritdoc />
		public void Report(IReadOnlyList<IDiagnosticResult> results, IReporterOptions reporterOptions)
		{
			if (results == null || results.Count == 0)
			{
				return;
			}

			foreach (var result in results)
			{
				uint sourceLine = uint.MaxValue;
				string sourceRef = null;
				if (result.Location != null)
				{
					sourceRef = reporterOptions.RootDirectory?.GetRelativePathTo(result.Location.Source);
					sourceLine = result.Location.SourceLine;
				}

				this.Report(result.Code, result.Message, sourceRef, sourceLine);
			}
		}
	}
}
