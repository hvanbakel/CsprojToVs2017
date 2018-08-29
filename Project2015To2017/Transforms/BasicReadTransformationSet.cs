using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Transforms
{
	public class BasicReadTransformationSet : ITransformationSet
	{
		public static readonly BasicReadTransformationSet Instance = new BasicReadTransformationSet();

		private BasicReadTransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				new NuGetPackageTransformation(),
				new AssemblyAttributeTransformation(logger, conversionOptions.KeepAssemblyInfo),
			};
		}
	}
}