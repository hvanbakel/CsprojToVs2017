using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public class NoopTransformationSet : ITransformationSet
	{
		public static readonly NoopTransformationSet Instance = new NoopTransformationSet();

		private NoopTransformationSet()
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return Array.Empty<ITransformation>();
		}
	}
}