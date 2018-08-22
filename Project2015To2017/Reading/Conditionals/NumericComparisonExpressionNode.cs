// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Evaluates a numeric comparison, such as less-than, or greater-or-equal-than
    /// Does not update conditioned properties table.
    /// </summary>
    internal abstract class NumericComparisonExpressionNode : OperatorExpressionNode
    {
        /// <summary>
        /// Compare numbers
        /// </summary>
        protected abstract bool Compare(double left, double right);

        /// <summary>
        /// Compare Versions. This is only intended to compare version formats like "A.B.C.D" which can otherwise not be compared numerically
        /// </summary>
        protected abstract bool Compare(Version left, Version right);

        /// <summary>
        /// Compare mixed numbers and Versions
        /// </summary>
        protected abstract bool Compare(Version left, double right);

        /// <summary>
        /// Compare mixed numbers and Versions
        /// </summary>
        protected abstract bool Compare(double left, Version right);

        /// <summary>
        /// Evaluate as boolean
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
            bool isLeftNum = this.LeftChild.CanNumericEvaluate(state);
            bool isLeftVersion = this.LeftChild.CanVersionEvaluate(state);
            bool isRightNum = this.RightChild.CanNumericEvaluate(state);
            bool isRightVersion = this.RightChild.CanVersionEvaluate(state);
            bool isNumeric = isLeftNum && isRightNum;
            bool isVersion = isLeftVersion && isRightVersion;

            // If the values identify as numeric, make that comparison instead of the Version comparison since numeric has a stricter definition
            if (isNumeric)
            {
                return Compare(this.LeftChild.NumericEvaluate(state), this.RightChild.NumericEvaluate(state));
            }
            else if (isVersion)
            {
                return Compare(this.LeftChild.VersionEvaluate(state), this.RightChild.VersionEvaluate(state));
            }

            // If the numbers are of a mixed type, call that specific Compare method
            if (isLeftNum && isRightVersion)
            {
                return Compare(this.LeftChild.NumericEvaluate(state), this.RightChild.VersionEvaluate(state));
            }
            else if (isLeftVersion && isRightNum)
            {
                return Compare(this.LeftChild.VersionEvaluate(state), this.RightChild.NumericEvaluate(state));
            }

            // Throw error here as this code should be unreachable
            ErrorUtilities.ThrowInternalErrorUnreachable();
            return false;
        }
    }
}
