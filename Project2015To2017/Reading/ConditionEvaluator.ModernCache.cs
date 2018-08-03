using System.Runtime.Caching;

namespace Project2015To2017.Reading
{
	/// <summary>
	/// 
	/// </summary>
	public static partial class ConditionEvaluator
    {
        private static bool TryGetCachedOrCreateState(string condition, out ConditionEvaluationStateImpl state)
        {
            state = MemoryCache.Default[condition] as ConditionEvaluationStateImpl;

            if (state != null)
            {
                return true;
            }

            state = new ConditionEvaluationStateImpl();

            MemoryCache.Default.Add(condition, state, ObjectCache.InfiniteAbsoluteExpiration);

            return false;
        }
    }
}