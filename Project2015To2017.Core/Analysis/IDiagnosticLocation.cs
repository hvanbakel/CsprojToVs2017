using System.IO;

namespace Project2015To2017.Analysis
{
	public interface IDiagnosticLocation
	{
		FileSystemInfo Source { get; }
		uint SourceLine { get; }
		string SourcePath { get; }
	}
}