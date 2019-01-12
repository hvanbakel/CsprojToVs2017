using Project2015To2017.Analysis;
using Project2015To2017.Migrate2017.Diagnostics;

namespace Project2015To2017.Migrate2017
{
	public static class Vs15DiagnosticSet
	{
		public static readonly IDiagnostic W020 = new W020MicrosoftCSharpDiagnostic();
		public static readonly IDiagnostic W021 = new W021SystemNuGetPackagesDiagnostic();

		public static readonly IDiagnostic W030 = new W030LegacyDebugTypesDiagnostic();
		public static readonly IDiagnostic W031 = new W031MSBuildSdkVersionSpecificationDiagnostic();
		public static readonly IDiagnostic W032 = new W032OldLanguageVersionDiagnostic();
		public static readonly IDiagnostic W033 = new W033ObsoletePortableClassLibrariesDiagnostic();
		public static readonly IDiagnostic W034 = new W034ReferenceAliasesDiagnostic();

		public static readonly DiagnosticSet ModernIssues = new DiagnosticSet
		{
			W020,
			W021,
		};

		public static readonly DiagnosticSet ModernizationTips = new DiagnosticSet
		{
			W030,
			W031,
			W032,
			W033,
			W034,
		};

		public static readonly DiagnosticSet All = new DiagnosticSet();

		static Vs15DiagnosticSet()
		{
			All.UnionWith(DiagnosticSet.All);
			All.UnionWith(ModernIssues);
			All.UnionWith(ModernizationTips);
		}
	}
}
