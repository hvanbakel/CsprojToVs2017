using System;
using System.Collections.Generic;
using Project2015To2017.Reading.Conditionals;

namespace Project2015To2017.Reading
{
	internal sealed class ConditionEvaluationStateImpl : IConditionEvaluationState
	{
		/// <inheritdoc />
		public Dictionary<string, List<string>> ConditionedPropertiesInProject { get; } =
			new Dictionary<string, List<string>>();

		public GenericExpressionNode Node { get; set; }

		public bool Evaluated { get; set; }

		public ICollection<OperatorExpressionNode> UnsupportedNodes { get; set; } =
			Array.Empty<OperatorExpressionNode>();

		public string Condition { get; }

		public ConditionEvaluationStateImpl(string condition)
		{
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
		}

		/// <inheritdoc />
		public string ExpandIntoStringBreakEarly(string expression)
		{
			return expression;
		}

		/// <inheritdoc />
		public string ExpandIntoString(string expression)
		{
			return expression;
		}

		public void Evaluate()
		{
			try
			{
				// it makes little sense for condition to be that short
				if (Condition.Length >= 2)
				{
					Node.Evaluate(this); // return value ignored
				}
			}
			catch (Exception)
			{
				// ignored
			}
			finally
			{
				Evaluated = true;
			}
		}
	}
}