// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Project2015To2017.Reading.Conditionals
{
	/// <summary>
	/// Node representing a string
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay,nq}")]
	internal sealed class StringExpressionNode : OperandExpressionNode
	{
		private string _value;
		private string _cachedExpandedValue;

		/// <summary>
		/// Whether the string potentially has expandable content,
		/// such as a property expression or escaped character.
		/// </summary>
		private bool _expandable;

		internal StringExpressionNode(string value, bool expandable)
		{
			this._value = value;
			this._expandable = expandable;
		}

		/// <summary>
		/// Evaluate as boolean
		/// </summary>
		internal override bool BoolEvaluate(IConditionEvaluationState state)
		{
			return ConversionUtilities.ConvertStringToBool(GetExpandedValue(state));
		}

		/// <summary>
		/// Evaluate as numeric
		/// </summary>
		internal override double NumericEvaluate(IConditionEvaluationState state)
		{
			return ConversionUtilities.ConvertDecimalOrHexToDouble(GetExpandedValue(state));
		}

		internal override Version VersionEvaluate(IConditionEvaluationState state)
		{
			return Version.Parse(GetExpandedValue(state));
		}

		internal override bool CanBoolEvaluate(IConditionEvaluationState state)
		{
			return ConversionUtilities.CanConvertStringToBool(GetExpandedValue(state));
		}

		internal override bool CanNumericEvaluate(IConditionEvaluationState state)
		{
			return ConversionUtilities.ValidDecimalOrHexNumber(GetExpandedValue(state));
		}

		internal override bool CanVersionEvaluate(IConditionEvaluationState state)
		{
			Version unused;
			return Version.TryParse(GetExpandedValue(state), out unused);
		}

		/// <summary>
		/// Returns true if this node evaluates to an empty string,
		/// otherwise false.
		/// It may be cheaper to determine whether an expression will evaluate
		/// to empty than to fully evaluate it.
		/// Implementations should cache the result so that calls after the first are free.
		/// </summary>
		internal override bool EvaluatesToEmpty(IConditionEvaluationState state)
		{
			if (this._cachedExpandedValue == null)
			{
				if (this._expandable)
				{
					string expandBreakEarly = state.ExpandIntoStringBreakEarly(this._value);

					if (expandBreakEarly == null)
					{
						// It broke early: we can't store the value, we just
						// know it's non empty
						return false;
					}

					// It didn't break early, the result is accurate,
					// so store it so the work isn't done again.
					this._cachedExpandedValue = expandBreakEarly;
				}
				else
				{
					this._cachedExpandedValue = this._value;
				}
			}

			return (this._cachedExpandedValue.Length == 0);
		}


		/// <summary>
		/// Value before any item and property expressions are expanded
		/// </summary>
		/// <returns></returns>
		internal override string GetUnexpandedValue(IConditionEvaluationState state)
		{
			return this._value;
		}

		/// <summary>
		/// Value after any item and property expressions are expanded
		/// </summary>
		/// <returns></returns>
		internal override string GetExpandedValue(IConditionEvaluationState state)
		{
			if (this._cachedExpandedValue == null)
			{
				if (this._expandable)
				{
					this._cachedExpandedValue = state.ExpandIntoString(this._value);
				}
				else
				{
					this._cachedExpandedValue = this._value;
				}
			}

			return this._cachedExpandedValue;
		}

		/// <summary>
		/// If any expression nodes cache any state for the duration of evaluation, 
		/// now's the time to clean it up
		/// </summary>
		internal override void ResetState()
		{
			this._cachedExpandedValue = null;
		}

		internal override string DebuggerDisplay => $"\"{this._value}\"";
	}
}
