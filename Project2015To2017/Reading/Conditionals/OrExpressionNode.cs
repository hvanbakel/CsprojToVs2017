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
            if (LeftChild.BoolEvaluate(state))
            {
                // Short circuit
                return true;
            }
            else
            {
                return RightChild.BoolEvaluate(state);
            }
        }

        internal override string DebuggerDisplay => $"(or {LeftChild.DebuggerDisplay} {RightChild.DebuggerDisplay})";

        #region REMOVE_COMPAT_WARNING
        private bool _possibleOrCollision = true;
        internal override bool PossibleOrCollision
        {
            set { _possibleOrCollision = value; }
            get { return _possibleOrCollision; }
        }
        #endregion
    }
}
