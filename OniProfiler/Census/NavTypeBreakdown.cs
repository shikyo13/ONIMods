using System.Collections.Generic;

namespace OniProfiler.Census
{
    /// <summary>
    /// Iterates LiveMinionIdentities to count dupes by NavType (walker vs flyer).
    /// Iteration is cheap — typically only ~8-30 dupes.
    /// </summary>
    public static class NavTypeBreakdown
    {
        /// <summary>
        /// Counts dupes currently using Jet Suit (hover) navigation.
        /// </summary>
        public static int CountFlyers(IList<MinionIdentity> liveMinions)
        {
            if (liveMinions == null || liveMinions.Count == 0)
                return 0;

            int flyers = 0;
            for (int i = 0; i < liveMinions.Count; i++)
            {
                var minion = liveMinions[i];
                if (minion == null) continue;

                var navigator = minion.GetComponent<Navigator>();
                if (navigator != null && navigator.CurrentNavType == NavType.Hover)
                    flyers++;
            }
            return flyers;
        }
    }
}
