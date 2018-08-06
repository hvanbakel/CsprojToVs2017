using Project2015To2017.Definition;

namespace Project2015To2017.Analysis
{
	public class DiagnosticResult : IDiagnosticResult
	{
		public string Code { get; internal set; }
		public string Message { get; internal set; }
		public Project Project { get; internal set; }
		public IDiagnosticLocation Location { get; internal set; }

		public DiagnosticResult()
		{
		}

		public DiagnosticResult(IDiagnosticResult result)
		{
			Code = result.Code;
			Message = result.Message;
			Project = result.Project;
			Location = result.Location;
		}
	}
}