// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Performs logical OR on children
    /// Does not update conditioned properties table
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    internal sealed class OrExpressionNode : OperatorExpressionNode
    {
        /// <summary>
        /// Evaluate as boolean
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
            if (this.LeftChild.BoolEvaluate(state))
            {
                // Short circuit
                return true;
            }
            else
            {
                return this.RightChild.BoolEvaluate(state);
            }
        }

        internal override string DebuggerDisplay => $"(or {this.LeftChild.DebuggerDisplay} {this.RightChild.DebuggerDisplay})";

        #region REMOVE_COMPAT_WARNING
        private bool _possibleOrCollision = true;
        internal override bool PossibleOrCollision
        {
            set { this._possibleOrCollision = value; }
            get { return this._possibleOrCollision; }
        }
        #endregion
    }
}
