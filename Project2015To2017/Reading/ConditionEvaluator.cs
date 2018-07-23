using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Project2015To2017.Reading.Conditionals;

namespace Project2015To2017.Reading
{
	/// <summary>
	/// 
	/// </summary>
	public static class ConditionEvaluator
	{
		#region MSBuild Conditional routine

		private static readonly Regex SinglePropertyRegex = new Regex(@"^\$\(([^\$\(\)]*)\)$", RegexOptions.Compiled);

		/// <summary>
		/// Update our table which keeps track of all the properties that are referenced
		/// inside of a condition and the string values that they are being tested against.
		/// So, for example, if the condition was " '$(Configuration)' == 'Debug' ", we
		/// would get passed in leftValue="$(Configuration)" and rightValueExpanded="Debug".
		/// This call would add the string "Debug" to the list of possible values for the
		/// "Configuration" property.
		///
		/// This method also handles the case when two or more properties are being
		/// concatenated together with a vertical bar, as in '
		///     $(Configuration)|$(Platform)' == 'Debug|x86'
		/// </summary>
		public static void UpdateConditionedPropertiesTable
		(
			Dictionary<string, List<string>>
				conditionedPropertiesTable, // List of possible values, keyed by property name
			string leftValue, // The raw value on the left side of the operator
			string rightValueExpanded // The fully expanded value on the right side
			// of the operator.
		)
		{
			if ((conditionedPropertiesTable != null) && (rightValueExpanded.Length > 0))
			{
				// The left side should be exactly "$(propertyname)" or "$(propertyname1)|$(propertyname2)"
				// or "$(propertyname1)|$(propertyname2)|$(propertyname3)", etc.  Anything else,
				// and we don't touch the table.

				// Split up the leftValue into pieces based on the vertical bar character.
				// PERF: Avoid allocations from string.Split by forming spans between 'pieceStart' and 'pieceEnd'
				var pieceStart = 0;

				// Loop through each of the pieces.
				while (true)
				{
					var pieceSeparator = leftValue.IndexOf('|', pieceStart);
					var lastPiece = pieceSeparator < 0;
					var pieceEnd = lastPiece ? leftValue.Length : pieceSeparator;

					var singlePropertyMatch =
						SinglePropertyRegex.Match(leftValue, pieceStart, pieceEnd - pieceStart);

					if (singlePropertyMatch.Success)
					{
						// Find the first vertical bar on the right-hand-side expression.
						var indexOfVerticalBar = rightValueExpanded.IndexOf('|');
						string rightValueExpandedPiece;

						// If there was no vertical bar, then just use the remainder of the right-hand-side
						// expression as the value of the property, and terminate the loop after this iteration.
						// Also, if we're on the last segment of the left-hand-side, then use the remainder
						// of the right-hand-side expression as the value of the property.
						if ((indexOfVerticalBar == -1) || lastPiece)
						{
							rightValueExpandedPiece = rightValueExpanded;
							lastPiece = true;
						}
						else
						{
							// If we found a vertical bar, then the portion before the vertical bar is the
							// property value which we will store in our table.  Then remove that portion
							// from the original string so that the next iteration of the loop can easily search
							// for the first vertical bar again.
							rightValueExpandedPiece = rightValueExpanded.Substring(0, indexOfVerticalBar);
							rightValueExpanded = rightValueExpanded.Substring(indexOfVerticalBar + 1);
						}

						// Capture the property name out of the regular expression.
						var propertyName = singlePropertyMatch.Groups[1].ToString();

						// Get the string collection for this property name, if one already exists.

						// If this property is not already represented in the table, add a new entry
						// for it.
						if (!conditionedPropertiesTable.TryGetValue(propertyName, out var conditionedPropertyValues))
						{
							conditionedPropertyValues = new List<string>();
							conditionedPropertiesTable[propertyName] = conditionedPropertyValues;
						}

						// If the "rightValueExpanded" is not already in the string collection
						// for this property name, add it now.
						if (!conditionedPropertyValues.Contains(rightValueExpandedPiece))
						{
							conditionedPropertyValues.Add(rightValueExpandedPiece);
						}
					}

					if (lastPiece)
					{
						break;
					}

					pieceStart = pieceSeparator + 1;
				}
			}
		}

		#endregion

		public static Dictionary<string, string> GetNonAmbiguousConditionContracts(string condition)
		{
			var res = new Dictionary<string, string>();

			// it makes little sense for condition to be that short
			if (condition.Length < 2)
				return res;

			foreach (var keyValuePair in GetConditionValues(condition))
			{
				if (keyValuePair.Value.Count != 1)
					continue;
				res.Add(keyValuePair.Key, keyValuePair.Value[0]);
			}

			return res;
		}

		public static Dictionary<string, List<string>> GetConditionValues(string condition)
		{
			var state = new ConditionEvaluationStateImpl();

			// it makes little sense for condition to be that short
			if (condition.Length < 2)
				return state.ConditionedPropertiesInProject;

			var parser = new Parser();
			try
			{
				var node = parser.Parse(condition, ParserOptions.AllowAll);
				node.Evaluate(state); // return value ignored
			}
			catch (Exception)
			{
				// ignored
			}

			return state.ConditionedPropertiesInProject;
		}

		private sealed class ConditionEvaluationStateImpl : IConditionEvaluationState
		{
			/// <inheritdoc />
			public Dictionary<string, List<string>> ConditionedPropertiesInProject { get; } = new Dictionary<string, List<string>>();

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
		}
	}
}