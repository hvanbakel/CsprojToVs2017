// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Project2015To2017.Reading.Conditionals
{
	/// <summary>
	/// Base class for all expression nodes.
	/// </summary>
	public abstract class GenericExpressionNode
    {
        internal abstract bool CanBoolEvaluate(IConditionEvaluationState state);
        internal abstract bool CanNumericEvaluate(IConditionEvaluationState state);
        internal abstract bool CanVersionEvaluate(IConditionEvaluationState state);
        internal abstract bool BoolEvaluate(IConditionEvaluationState state);
        internal abstract double NumericEvaluate(IConditionEvaluationState state);
        internal abstract Version VersionEvaluate(IConditionEvaluationState state);

        /// <summary>
        /// Returns true if this node evaluates to an empty string,
        /// otherwise false.
        /// (It may be cheaper to determine whether an expression will evaluate
        /// to empty than to fully evaluate it.)
        /// Implementations should cache the result so that calls after the first are free.
        /// </summary>
        internal virtual bool EvaluatesToEmpty(IConditionEvaluationState state)
        {
            return false;
        }

        /// <summary>
        /// Value after any item and property expressions are expanded
        /// </summary>
        /// <returns></returns>
        internal abstract string GetExpandedValue(IConditionEvaluationState state);

        /// <summary>
        /// Value before any item and property expressions are expanded
        /// </summary>
        /// <returns></returns>
        internal abstract string GetUnexpandedValue(IConditionEvaluationState state);

        /// <summary>
        /// If any expression nodes cache any state for the duration of evaluation, 
        /// now's the time to clean it up
        /// </summary>
        internal abstract void ResetState();

        /// <summary>
        /// The main evaluate entry point for expression trees
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        internal bool Evaluate(IConditionEvaluationState state)
        {
            return BoolEvaluate(state);
        }

        /// <summary>
        /// Get display string for this node for use in the debugger.
        /// </summary>
        internal virtual string DebuggerDisplay { get; }


        #region REMOVE_COMPAT_WARNING
        internal virtual bool PossibleAndCollision
        {
            set { /* do nothing */ }
            get { return false; }
        }

        internal virtual bool PossibleOrCollision
        {
            set { /* do nothing */ }
            get { return false; }
        }

        internal abstract bool DetectOr();
        internal abstract bool DetectAnd();
        #endregion

    }
}
