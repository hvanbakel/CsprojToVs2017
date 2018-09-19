using Project2015To2017.Analysis.Diagnostics;
using System.Collections.Generic;

namespace Project2015To2017.Analysis
{
	public sealed class DiagnosticSet : HashSet<IDiagnostic>
	{
		public static readonly IDiagnostic W001 = new W001IllegalProjectTypeDiagnostic();
		public static readonly IDiagnostic W002 = new W002MissingProjectFileDiagnostic();
		public static readonly IDiagnostic W010 = new W010ConfigurationsMismatchDiagnostic();
		public static readonly IDiagnostic W011 = new W011UnsupportedConditionalDiagnostic();

		public static readonly DiagnosticSet NoneDefault = new DiagnosticSet();

		public static readonly DiagnosticSet System = new DiagnosticSet
		{
			W001,
			W002,
		};

		public static readonly DiagnosticSet GenericProjectIssues = new DiagnosticSet
		{
			W010,
			W011,
		};

		public static readonly DiagnosticSet All = new DiagnosticSet();

		static DiagnosticSet()
		{
			All.UnionWith(System);
			All.UnionWith(GenericProjectIssues);
		}
	}
}