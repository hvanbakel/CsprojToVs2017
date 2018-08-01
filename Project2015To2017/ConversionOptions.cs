using System.Collections.Generic;

namespace Project2015To2017
{
    public class ConversionOptions
    {
	    /// <summary>
	    /// Whether to keep the AssemblyInfo.cs file, or to
	    /// move the attributes into the project file
	    /// </summary>
	    public bool KeepAssemblyInfo { get; set; }
		/// <summary>
		/// Change the target framework to a specific framework, or to
		/// multi target frameworks
		/// </summary>
		public IReadOnlyList<string> TargetFrameworks { get; set; }
		/// <summary>
		/// Append the target framework to the output path
		/// </summary>
		public bool AppendTargetFrameworkToOutputPath { get; set; } = true;
	}
}
