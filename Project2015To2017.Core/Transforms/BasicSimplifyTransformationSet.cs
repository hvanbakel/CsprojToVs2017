using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Project2015To2017.Transforms
{
	public sealed class BasicSimplifyTransformationSet : ITransformationSet
	{
		private readonly Version targetVisualStudioVersion;

		public BasicSimplifyTransformationSet(Version targetVisualStudioVersion)
		{
			this.targetVisualStudioVersion = targetVisualStudioVersion;
		}

		public IReadOnlyCollection<ITransformation> Transformations(
			ILogger logger,
			ConversionOptions conversionOptions)
		{
			return new ITransformation[]
			{
				new PropertyDeduplicationTransformation(),
				new PropertySimplificationTransformation(targetVisualStudioVersion),
				new ServiceFilterTransformation(targetVisualStudioVersion),
				new PrimaryProjectPropertiesUpdateTransformation(),
				new EmptyGroupRemoveTransformation(),
			};
		}
	}
}
