using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public class BasicTransformationSet : ITransformationSet
	{
		private readonly IReadOnlyCollection<ITransformation> transformations;

		public BasicTransformationSet(IReadOnlyCollection<ITransformation> transformations)
		{
			this.transformations = transformations;
		}

		public BasicTransformationSet(params ITransformation[] transformations)
			: this((IReadOnlyCollection<ITransformation>)transformations)
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(ILogger logger, ConversionOptions conversionOptions)
		{
			return transformations;
		}
	}
}
