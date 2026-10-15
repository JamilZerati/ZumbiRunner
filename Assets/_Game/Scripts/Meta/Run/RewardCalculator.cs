using System;
using Game.Core.State;
using Game.Meta.Rewards;

namespace Game.Meta.Run
{
    public class RewardCalculator : IRewardCalculator
    {
        public RewardBundle Calculate(RunResult result, RunConfig config, bool doubleReward = false) => throw new NotImplementedException();
    }
}
