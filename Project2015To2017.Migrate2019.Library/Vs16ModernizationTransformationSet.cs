using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2017;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2019.Library
{
	public sealed class Vs16ModernizationTransformationSet : ITransformationSet
	{
		public static readonly Vs16ModernizationTransformationSet TrueInstance =
			new Vs16ModernizationTransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
			new BasicSimplifyTransformationSet(Vs16TransformationSet.TargetVisualStudioVersion),
			Vs15ModernizationTransformationSet.TrueInstance,
			TrueInstance);

		private Vs16ModernizationTransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
			};
		}
	}
}