using System.Collections.Generic;

namespace Game.Meta.Ads
{
    public class AdPolicyState
    {
        public long LastAdShownUtcTicks { get; set; }
        public long LastRewardedOrPurchaseUtcTicks { get; set; }
        public long LastInterstitialUtcTicks { get; set; }
        public int StagesCompletedSinceLastInterstitial { get; set; }
        public int StagesCompletedInCurrentSession { get; set; }
        public long CurrentSessionDateUtcTicks { get; set; }
        public int SessionsStartedInCurrentDay { get; set; }
        public int DailyRewardedCount { get; set; }
        public long DailyRewardedDateUtcTicks { get; set; }
        public Dictionary<string, int> PlacementDailyCounts { get; set; } = new Dictionary<string, int>();
    }
}
