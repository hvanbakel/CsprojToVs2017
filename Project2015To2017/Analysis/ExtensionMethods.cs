using System.Globalization;

namespace Project2015To2017.Analysis
{
	internal static class ExtensionMethods
	{
		public static string ToDiagnosticCode(this uint id) => $"W{id.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0')}";
	}
}