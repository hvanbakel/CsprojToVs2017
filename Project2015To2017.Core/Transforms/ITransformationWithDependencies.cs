using System.Collections.Generic;

namespace Project2015To2017.Transforms
{
	public interface ITransformationWithDependencies : ITransformation
	{
		IReadOnlyCollection<string> DependOn { get; }
	}
}