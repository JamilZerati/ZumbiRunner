using System;
using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Wallet;

namespace Game.Meta.Run
{
    public class RewardCalculator : IRewardCalculator
    {
        private readonly IReadOnlyDictionary<string, float> _mutationBonuses;

        public RewardCalculator(IReadOnlyDictionary<string, float> mutationBonuses = null)
        {
            _mutationBonuses = mutationBonuses;
        }

        public RewardBundle Calculate(RunResult result, RunConfig config, bool doubleReward = false)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            float multiplierReached = result.MultiplierReached > 0 ? result.MultiplierReached : 1.0f;
            float configMultiplier = config != null && config.RewardMultiplier > 0 ? config.RewardMultiplier : 1.0f;

            float mutationBonusSum = 0f;
            if (config?.MutationIds != null && _mutationBonuses != null)
            {
                foreach (string mutationId in config.MutationIds)
                {
                    if (_mutationBonuses.TryGetValue(mutationId, out float bonus))
                    {
                        mutationBonusSum += bonus;
                    }
                }
            }

            float effectiveMultiplier = multiplierReached * configMultiplier * (1.0f + mutationBonusSum);
            if (doubleReward)
            {
                effectiveMultiplier *= 2.0f;
            }

            int finalCoins = (int)Math.Round(result.CoinsEarned * effectiveMultiplier);
            string levelId = result.LevelId ?? config?.LevelId ?? "stage";
            string grantId = $"run_{levelId}_{result.CoinsEarned}_{multiplierReached}_{result.Stars}_{(doubleReward ? "2x" : "1x")}";

            var rewards = new List<IReward>
            {
                new CurrencyReward(CurrencyKind.Coins, finalCoins)
            };

            return new RewardBundle(grantId, rewards);
        }
    }
}
