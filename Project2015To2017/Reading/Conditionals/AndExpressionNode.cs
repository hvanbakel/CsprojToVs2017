// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Performs logical AND on children
    /// Does not update conditioned properties table
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class AndExpressionNode : OperatorExpressionNode
    {
        /// <summary>
        /// Evaluate as boolean
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
            if (!this.LeftChild.BoolEvaluate(state))
            {
                // Short circuit
                return false;
            }
            else
            {
                return this.RightChild.BoolEvaluate(state);
            }
        }

        internal override string DebuggerDisplay => $"(and {this.LeftChild.DebuggerDisplay} {this.RightChild.DebuggerDisplay})";

        #region REMOVE_COMPAT_WARNING
        private bool _possibleAndCollision = true;
        internal override bool PossibleAndCollision
        {
            set { this._possibleAndCollision = value; }
            get { return this._possibleAndCollision; }
        }
        #endregion
    }
}
