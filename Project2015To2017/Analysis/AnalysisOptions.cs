using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Project2015To2017.Analysis
{
	public class AnalysisOptions
	{
		/// <summary>
		/// Option set used for conversion, if any
		/// </summary>
		public ConversionOptions ConversionOptions { get; set; }
		/// <summary>
		/// Basement of all relative roots in output
		/// </summary>
		public DirectoryInfo RootDirectory { get; set; }
		/// <summary>
		/// Including ID of diagnostics in this list will make analyzer skip their execution and therefore output
		/// </summary>
		public ImmutableHashSet<DiagnosticBase> Diagnostics { get; }

		public AnalysisOptions(IEnumerable<DiagnosticBase> diagnostics = null)
		{
			Diagnostics = (diagnostics ?? DiagnosticSet.AllDefault).ToImmutableHashSet();
		}
	}
}
