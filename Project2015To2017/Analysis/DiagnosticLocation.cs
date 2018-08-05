using System.IO;

namespace Project2015To2017.Analysis
{
	public class DiagnosticLocation : IDiagnosticLocation
	{
		public FileSystemInfo Source { get; set; }
		public uint SourceLine { get; set; }
	}
}