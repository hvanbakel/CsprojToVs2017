// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Project2015To2017.Reading.Conditionals
{
    /// <summary>
    /// Evaluates a function expression, such as "Exists('foo')"
    /// </summary>
    internal sealed class FunctionCallExpressionNode : OperatorExpressionNode
    {
        private readonly List<GenericExpressionNode> _arguments;
        public readonly string _functionName;

        internal FunctionCallExpressionNode(string functionName, List<GenericExpressionNode> arguments)
        {
            _functionName = functionName;
            _arguments = arguments;
        }

        /// <summary>
        /// Evaluate node as boolean
        /// </summary>
        internal override bool BoolEvaluate(IConditionEvaluationState state)
        {
	        if (String.Compare(_functionName, "exists", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

	        if (String.Compare(_functionName, "HasTrailingSlash", StringComparison.OrdinalIgnoreCase) == 0)
	        {
		        // often used to append slash to path so return false to enable this codepath
		        return false;
	        }

	        // We haven't implemented any other "functions"

	        return false;
        }
    }
}
