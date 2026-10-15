namespace Game.Meta.Rewards
{
    public interface IReward
    {
        string RewardType { get; }
        void Apply(IRewardSink sink);
    }
}
