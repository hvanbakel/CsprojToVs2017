using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public sealed class ConversionOptions
	{
		/// <summary>
		/// Project cache, if any. When null no caching is used.
		/// </summary>
		public Caching.IProjectCache ProjectCache { get; set; }
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
		[Obsolete("Use ITransformationSet as transformation collection, inherit transformations from ITransformationWithTargetMoment to set Early execution moment.")]
		public IReadOnlyList<ITransformation> PreDefaultTransforms { get; set; } = ImmutableArray<ITransformation>.Empty;
		/// <summary>
		/// A collection of transforms executed after the execution of default ones
		/// </summary>
		[Obsolete("Use ITransformationSet as transformation collection, inherit transformations from ITransformationWithTargetMoment to set Late execution moment.")]
		public IReadOnlyList<ITransformation> PostDefaultTransforms { get; set; } = ImmutableArray<ITransformation>.Empty;
		/// <summary>
		/// A collection of transform class names executed despite being intended for different project system,
		/// like forcing <see cref="ILegacyOnlyProjectTransformation"/> run on already converted project.
		/// </summary>
		public IReadOnlyList<string> ForceDefaultTransforms { get; set; } = ImmutableArray<string>.Empty;
		/// <summary>
		/// Action that will be executed when project reader cannot unambiguously determine target frameworks
		/// for the currently processing project.
		/// </summary>
		public UnknownTargetFrameworkCallback UnknownTargetFrameworkCallback { get; set; }

		/// <summary>
		/// Force conversion ignoring any checks we might do that prevent a conversion.
		/// </summary>
		[Obsolete("Use ForceOnUnsupportedProjects instead", true)]
		public bool Force
		{
			get => ForceOnUnsupportedProjects;
			set => ForceOnUnsupportedProjects = value;
		}

		/// <summary>
		/// Force conversion of projects otherwise considered of an unsupported type.
		/// </summary>
		public bool ForceOnUnsupportedProjects { get; set; }
	}
}
