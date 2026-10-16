using Game.Core.State;
using Game.Meta.Rewards;

namespace Game.Meta.Run
{
    public interface IRewardCalculator
    {
        RewardBundle Calculate(RunResult result, RunConfig config, bool doubleReward = false);
    }
}
