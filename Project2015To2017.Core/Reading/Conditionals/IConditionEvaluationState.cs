using System.Collections.Generic;

namespace Project2015To2017.Reading.Conditionals
{
	internal interface IConditionEvaluationState
	{
		/// <summary>
		///     Table of conditioned properties and their values.
		///     Used to populate configuration lists in some project systems.
		///     If this is null, as it is for command line builds, conditioned properties
		///     are not recorded.
		/// </summary>
		Dictionary<string, List<string>> ConditionedPropertiesInProject { get; }

		/// <summary>
		///     May return null if the expression would expand to non-empty and it broke out early.
		///     Otherwise, returns the correctly expanded expression.
		/// </summary>
		string ExpandIntoStringBreakEarly(string expression);

		/// <summary>
		///     Expands the specified expression into a string.
		/// </summary>
		string ExpandIntoString(string expression);
	}
}