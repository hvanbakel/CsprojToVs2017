namespace Project2015To2017.Transforms
{
	public interface ITransformationWithTargetMoment : ITransformation
	{
		TargetTransformationExecutionMoment ExecutionMoment { get; }
	}
}