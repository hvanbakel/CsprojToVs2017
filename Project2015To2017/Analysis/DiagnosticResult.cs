namespace Project2015To2017.Analysis
{
	public class DiagnosticResult : IDiagnosticResult
	{
		public string Code { get; internal set; }
		public string Message { get; internal set; }
		public IDiagnosticLocation Location { get; internal set; }
	}
}