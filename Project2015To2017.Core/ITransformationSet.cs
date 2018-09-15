using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Project2015To2017.Transforms;

namespace Project2015To2017
{
	public interface ITransformationSet
	{
		IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions);
	}
}