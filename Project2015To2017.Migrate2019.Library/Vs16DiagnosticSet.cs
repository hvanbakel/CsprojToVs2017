using Project2015To2017.Analysis;
using Project2015To2017.Migrate2017;

namespace Project2015To2017.Migrate2019.Library
{
	public static class Vs16DiagnosticSet
	{
		public static readonly DiagnosticSet All = new DiagnosticSet();

		static Vs16DiagnosticSet()
		{
			All.UnionWith(Vs15DiagnosticSet.All);
		}
	}
}
