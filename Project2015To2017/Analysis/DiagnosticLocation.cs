using System.IO;

namespace Project2015To2017.Analysis
{
	public class DiagnosticLocation : IDiagnosticLocation
	{
		public uint SourceLine { get; set; }
		public FileSystemInfo Source { get; set; }
		public string SourcePath { get; set; }

		public DiagnosticLocation()
		{
		}

		public DiagnosticLocation(IDiagnosticLocation location)
		{
			this.SourceLine = location.SourceLine;
			this.Source = location.Source;
			this.SourcePath = location.SourcePath;
		}
	}
}