using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2019.Library
{
	public sealed class Vs16TransformationSet : ITransformationSet
	{
		public static readonly Vs16TransformationSet TrueInstance = new Vs16TransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
			TrueInstance);

		private Vs16TransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			var targetVisualStudioVersion = new Version(16, 0);
			return new ITransformation[]
			{
				// Generic
				new TargetFrameworkReplaceTransformation(
					conversionOptions.TargetFrameworks,
					conversionOptions.AppendTargetFrameworkToOutputPath),
				new PropertyDeduplicationTransformation(),
				new PropertySimplificationTransformation(targetVisualStudioVersion),
				new PrimaryProjectPropertiesUpdateTransformation(),
				new EmptyGroupRemoveTransformation(),
				// VS15 migration
				new FrameworkReferencesTransformation(),
				new TestProjectPackageReferenceTransformation(logger),
				new AssemblyFilterPackageReferencesTransformation(),
				new AssemblyFilterHintedPackageReferencesTransformation(),
				new AssemblyFilterDefaultTransformation(),
				new ImportsTargetsFilterPackageReferencesTransformation(),
				new FileTransformation(logger),
				new XamlPagesTransformation(logger),
				new BrokenHookTargetsTransformation(logger),
			};
		}
	}
}