using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public class ChainTransformationSet : ITransformationSet
	{
		private readonly IReadOnlyCollection<ITransformationSet> sets;

		public ChainTransformationSet(IReadOnlyCollection<ITransformationSet> sets)
		{
			this.sets = sets ?? throw new ArgumentNullException(nameof(sets));
		}

		public ChainTransformationSet(params ITransformationSet[] sets)
			: this((IReadOnlyCollection<ITransformationSet>) sets)
		{
		}

		public IReadOnlyCollection<ITransformation> Transformations(ILogger logger, ConversionOptions conversionOptions)
		{
			var res = new List<ITransformation>();
			foreach (var set in sets)
			{
				res.AddRange(set.Transformations(logger, conversionOptions));
			}

			return res;
		}
	}
}