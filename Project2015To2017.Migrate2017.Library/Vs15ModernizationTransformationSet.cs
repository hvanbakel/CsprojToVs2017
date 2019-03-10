using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Migrate2017.Transforms;
using Project2015To2017.Transforms;

namespace Project2015To2017.Migrate2017
{
	public sealed class Vs15ModernizationTransformationSet : ITransformationSet
	{
		public static readonly Vs15ModernizationTransformationSet TrueInstance =
			new Vs15ModernizationTransformationSet();

		public static readonly ITransformationSet Instance = new ChainTransformationSet(
			BasicReadTransformationSet.Instance,
			TrueInstance);

		private Vs15ModernizationTransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				// Generic
				new PrimaryProjectPropertiesUpdateTransformation(),
				new EmptyGroupRemoveTransformation(),
				// Modernization
				new UpgradeDebugTypeTransformation(),
				new UpgradeUseDefaultOutputPathTransformation(),
				new UpgradeUseComVisibleDefaultTransformation(),
				new UpgradeTestServiceTransformation(),
				new UpgradeFrameworkAssembliesToNuGetTransformation(logger),
			};
		}
	}
}
