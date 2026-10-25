using System.Collections.Generic;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using Game.Meta.Wallet;

namespace Game.Meta.Debut
{
    public static class DebutJourneyDefinition
    {
        public const string TrackId = "debut_journey";

        public static ProgressTrackDefinition CreateDefaultTrack()
        {
            var milestones = new List<MilestoneDefinition>();
            int[] coinsPerDay = { 100, 150, 200, 250, 300, 400, 500 };

            for (int i = 0; i < 7; i++)
            {
                var rewards = new List<IReward>
                {
                    new CurrencyReward(CurrencyKind.Coins, coinsPerDay[i])
                };
                var bundle = new RewardBundle($"debut_day_{i + 1}", rewards);
                milestones.Add(new MilestoneDefinition(i, i + 1, bundle));
            }

            return new ProgressTrackDefinition(TrackId, milestones);
        }
    }
}
