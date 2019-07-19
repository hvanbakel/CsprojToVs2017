using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Migrate2019.Library.Transforms;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2019.Library
{
	public sealed class Vs16TransformationSet : ITransformationSet
	{
		public static readonly Version TargetVisualStudioVersion = new Version(16, 0);

		public static readonly Vs16TransformationSet TrueInstance = new Vs16TransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
			new BasicSimplifyTransformationSet(TargetVisualStudioVersion),
			TrueInstance);

		private Vs16TransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				// Generic
				new TargetFrameworkReplaceTransformation(
					conversionOptions.TargetFrameworks,
					conversionOptions.AppendTargetFrameworkToOutputPath),
				// VS16 migration
				new Vs16FrameworkReferencesTransformation(),
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