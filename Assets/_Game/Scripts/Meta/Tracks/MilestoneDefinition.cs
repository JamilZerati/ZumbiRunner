using Game.Meta.Rewards;

namespace Game.Meta.Tracks
{
    public class MilestoneDefinition
    {
        public int Index { get; }
        public int RequiredPoints { get; }
        public RewardBundle FreeReward { get; }
        public RewardBundle PremiumReward { get; }

        public MilestoneDefinition(int index, int requiredPoints, RewardBundle freeReward, RewardBundle premiumReward = null)
        {
            Index = index;
            RequiredPoints = requiredPoints;
            FreeReward = freeReward;
            PremiumReward = premiumReward;
        }
    }
}
