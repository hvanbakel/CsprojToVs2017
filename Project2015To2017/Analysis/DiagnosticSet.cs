using System.Collections.Generic;
using Project2015To2017.Analysis.Diagnostics;

namespace Project2015To2017.Analysis
{
	public class DiagnosticSet : HashSet<IDiagnostic>
	{
		public static readonly IDiagnostic W001 = new W001IllegalProjectTypeDiagnostic();
		// W002 is not a real diagnostic
		public static readonly IDiagnostic W010 = new W010ConfigurationsMismatchDiagnostic();
		public static readonly IDiagnostic W011 = new W011UnsupportedConditionalDiagnostic();
		public static readonly IDiagnostic W020 = new W020MicrosoftCSharpDiagnostic();
		public static readonly IDiagnostic W021 = new W021SystemNuGetPackagesDiagnostic();
		public static readonly IDiagnostic W030 = new W030LegacyDebugTypesDiagnostic();
		public static readonly IDiagnostic W031 = new W031MSBuildSdkVersionSpecificationDiagnostic();
		public static readonly IDiagnostic W032 = new W032OldLanguageVersionDiagnostic();
		public static readonly IDiagnostic W033 = new W033ObsoletePortableClassLibrariesDiagnostic();
		public static readonly IDiagnostic W034 = new W034ReferenceAliasesDiagnostic();

		public static readonly DiagnosticSet AllDefault = new DiagnosticSet
		{
			W001,
			W010,
			W011,
			W020,
			W021,
			W030,
			W031,
			W032,
			W033,
			W034,
		};

		public static readonly DiagnosticSet NoneDefault = new DiagnosticSet();
	}
}