using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Transforms
{
	public class Vs15TransformationSet : ITransformationSet
	{
		public static readonly Vs15TransformationSet TrueInstance = new Vs15TransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
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
				new PropertyDeduplicationTransformation(),
				new PropertySimplificationTransformation(),
				new PrimaryProjectPropertiesUpdateTransformation(),
				new EmptyGroupRemoveTransformation(),
				// VS15 migration
				new TestProjectPackageReferenceTransformation(logger),
				new AssemblyFilterPackageReferencesTransformation(),
				new AssemblyFilterHintedPackageReferencesTransformation(),
				new AssemblyFilterDefaultTransformation(),
				new ImportsTargetsFilterPackageReferencesTransformation(),
				new FileTransformation(logger),
				new XamlPagesTransformation(logger),
			};
		}
	}
}