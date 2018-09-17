// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Evaluates as boolean and evaluates children as boolean, numeric, or string.
    /// Order in which comparisons are attempted is numeric, boolean, then string.
    /// Updates conditioned properties table.
    /// </summary>
    internal abstract class MultipleComparisonNode : OperatorExpressionNode
    {
        private bool _conditionedPropertiesUpdated = false;

        /// <summary>
        /// Compare numbers
        /// </summary>
        protected abstract bool Compare(double left, double right);

        /// <summary>
        /// Compare booleans
        /// </summary>
        protected abstract bool Compare(bool left, bool right);

        /// <summary>
        /// Compare strings
        /// </summary>
        protected abstract bool Compare(string left, string right);

        /// <summary>
        /// Evaluates as boolean and evaluates children as boolean, numeric, or string.
        /// Order in which comparisons are attempted is numeric, boolean, then string.
        /// Updates conditioned properties table.
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
            // It's sometimes possible to bail out of expansion early if we just need to know whether 
            // the result is empty string.
            // If at least one of the left or the right hand side will evaluate to empty, 
            // and we know which do, then we already have enough information to evaluate this expression.
            // That means we don't have to fully expand a condition like " '@(X)' == '' " 
            // which is a performance advantage if @(X) is a huge item list.
            if (this.LeftChild.EvaluatesToEmpty(state) || this.RightChild.EvaluatesToEmpty(state))
            {
                UpdateConditionedProperties(state);

                return Compare(this.LeftChild.EvaluatesToEmpty(state), this.RightChild.EvaluatesToEmpty(state));
            }

            if (this.LeftChild.CanNumericEvaluate(state) && this.RightChild.CanNumericEvaluate(state))
            {
                return Compare(this.LeftChild.NumericEvaluate(state), this.RightChild.NumericEvaluate(state));
            }
            else if (this.LeftChild.CanBoolEvaluate(state) && this.RightChild.CanBoolEvaluate(state))
            {
                return Compare(this.LeftChild.BoolEvaluate(state), this.RightChild.BoolEvaluate(state));
            }
            else // string comparison
            {
                string leftExpandedValue = this.LeftChild.GetExpandedValue(state);
                string rightExpandedValue = this.RightChild.GetExpandedValue(state);
				
                UpdateConditionedProperties(state);

                return Compare(leftExpandedValue, rightExpandedValue);
            }
        }

        /// <summary>
        /// Reset temporary state
        /// </summary>
        internal override void ResetState()
        {
            base.ResetState();
			this._conditionedPropertiesUpdated = false;
        }

        /// <summary>
        /// Updates the conditioned properties table if it hasn't already been done.
        /// </summary>
        private void UpdateConditionedProperties(IConditionEvaluationState state)
        {
            if (!this._conditionedPropertiesUpdated && state.ConditionedPropertiesInProject != null)
            {
                string leftUnexpandedValue = this.LeftChild.GetUnexpandedValue(state);
                string rightUnexpandedValue = this.RightChild.GetUnexpandedValue(state);

                if (leftUnexpandedValue != null)
                {
                    ConditionEvaluator.UpdateConditionedPropertiesTable
                        (state.ConditionedPropertiesInProject,
                         leftUnexpandedValue,
						 this.RightChild.GetExpandedValue(state));
                }

                if (rightUnexpandedValue != null)
                {
                    ConditionEvaluator.UpdateConditionedPropertiesTable
                        (state.ConditionedPropertiesInProject,
                         rightUnexpandedValue,
						 this.LeftChild.GetExpandedValue(state));
                }

				this._conditionedPropertiesUpdated = true;
            }
        }
    }
}
