using System.Collections.Generic;
using System.Collections.Immutable;
using Project2015To2017.Transforms;

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
	    /// <summary>
	    /// A collection of transforms executed before the execution of default ones
	    /// </summary>
	    public IReadOnlyCollection<ITransformation> PreDefaultTransforms { get; set; } = ImmutableArray<ITransformation>.Empty;
	    /// <summary>
	    /// A collection of transforms executed after the execution of default ones
	    /// </summary>
	    public IReadOnlyCollection<ITransformation> PostDefaultTransforms { get; set; } = ImmutableArray<ITransformation>.Empty;
	}
}
