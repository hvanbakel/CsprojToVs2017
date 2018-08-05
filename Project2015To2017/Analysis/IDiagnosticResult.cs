namespace Project2015To2017.Analysis
{
	public interface IDiagnosticResult
	{
		string Code { get; }
		IDiagnosticLocation Location { get; }
		string Message { get; }
	}
}