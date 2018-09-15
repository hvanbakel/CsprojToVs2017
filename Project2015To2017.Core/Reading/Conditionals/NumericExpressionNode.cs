// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Represents a number - evaluates as numeric.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class NumericExpressionNode : OperandExpressionNode
    {
        private string _value;

        private NumericExpressionNode() { }

        internal NumericExpressionNode(string value)
        {
			this._value = value;
        }

        /// <summary>
        /// Evaluate as boolean
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
            // Should be unreachable: all calls check CanBoolEvaluate() first
            ErrorUtilities.VerifyThrow(false, "Can't evaluate a numeric expression as boolean.");
            return false;
        }

        /// <summary>
        /// Evaluate as numeric
        /// </summary>
        internal override double NumericEvaluate(IConditionEvaluationState state)
        {
            return ConversionUtilities.ConvertDecimalOrHexToDouble(this._value);
        }

        /// <summary>
        /// Evaluate as a Version
        /// </summary>
        internal override Version VersionEvaluate(IConditionEvaluationState state)
        {
            return Version.Parse(this._value);
        }

        /// <summary>
        /// Whether it can be evaluated as a boolean: never allowed for numerics
        /// </summary>
        internal override bool CanBoolEvaluate(IConditionEvaluationState state)
        {
            // Numeric expressions are never allowed to be treated as booleans.
            return false;
        }

        /// <summary>
        /// Whether it can be evaluated as numeric
        /// </summary>
        internal override bool CanNumericEvaluate(IConditionEvaluationState state)
        {
            // It is not always possible to numerically evaluate even a numerical expression -
            // for example, it may overflow a double. So check here.
            return ConversionUtilities.ValidDecimalOrHexNumber(this._value);
        }

        /// <summary>
        /// Whether it can be evaluated as a Version
        /// </summary>
        internal override bool CanVersionEvaluate(IConditionEvaluationState state)
        {
            // Check if the value can be formatted as a Version number
            // This is needed for nodes that identify as Numeric but can't be parsed as numbers (e.g. 8.1.1.0 vs 8.1)
            Version unused;
            return Version.TryParse(this._value, out unused);
        }

        /// <summary>
        /// Get the unexpanded value
        /// </summary>
        internal override string GetUnexpandedValue(IConditionEvaluationState state)
        {
            return this._value;
        }

        /// <summary>
        /// Get the expanded value
        /// </summary>
        internal override string GetExpandedValue(IConditionEvaluationState state)
        {
            return this._value;
        }

        /// <summary>
        /// If any expression nodes cache any state for the duration of evaluation, 
        /// now's the time to clean it up
        /// </summary>
        internal override void ResetState()
        {
        }

        internal override string DebuggerDisplay => $"#\"{this._value}\")";
    }
}
