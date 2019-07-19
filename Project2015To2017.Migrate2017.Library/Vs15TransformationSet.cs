using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017
{
	public sealed class Vs15TransformationSet : ITransformationSet
	{
		public static readonly Version TargetVisualStudioVersion = new Version(15, 0);

		public static readonly Vs15TransformationSet TrueInstance = new Vs15TransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
			new BasicSimplifyTransformationSet(TargetVisualStudioVersion),
			TrueInstance);

		private Vs15TransformationSet()
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