using System.IO;
using Project2015To2017.Transforms;

namespace Project2015To2017.Analysis
{
	public abstract class ReporterBase : IReporter
	{
		protected internal AnalysisOptions Options { get; set; }

		/// <inheritdoc />
		protected ReporterBase(AnalysisOptions options = null)
		{
			Options = options ?? new AnalysisOptions();
		}

		/// <inheritdoc />
		public abstract void Report(string code, string message, string source = null, uint sourceLine = uint.MaxValue);

		/// <inheritdoc />
		public virtual void Report(string code, string message, FileSystemInfo source = null, uint sourceLine = uint.MaxValue)
		{
			string sourceRef = null;
			if (source != null)
			{
				sourceRef = Options.RootDirectory?.GetRelativePathTo(source);
			}

			Report(code, message, sourceRef, sourceLine);
		}
	}
}
