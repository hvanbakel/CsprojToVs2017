using System.Collections.Generic;

namespace Project2015To2017.Reading
{
	/// <summary>
	/// 
	/// </summary>
	public static partial class ConditionEvaluator
    {
        private static readonly Dictionary<string, ConditionEvaluationStateImpl> Cache = new Dictionary<string, ConditionEvaluationStateImpl>();

        private static bool TryGetCachedOrCreateState(string condition, out ConditionEvaluationStateImpl state)
        {
            if (Cache.TryGetValue(condition, out state))
            {
                return true;
            }

            state = new ConditionEvaluationStateImpl();

            Cache.Add(condition, state);

            return false;
        }
    }
}