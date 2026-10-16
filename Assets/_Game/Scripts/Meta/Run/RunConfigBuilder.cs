using System;
using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Save;

namespace Game.Meta.Run
{
    public class RunConfigBuilder : IRunConfigBuilder
    {
        private const int DefaultInitialSquad = 10;
        private const string DefaultWeaponId = "rifle";
        private const float DefaultRewardMultiplier = 1.0f;

        private readonly MetaState _metaState;

        public RunConfigBuilder(MetaState metaState = null)
        {
            _metaState = metaState;
        }

        public RunConfig Build(string stageId, IReadOnlyList<string> selectedMutations = null, IReadOnlyList<string> activeBoosts = null)
        {
            if (string.IsNullOrWhiteSpace(stageId))
            {
                throw new ArgumentException("StageId is required.", nameof(stageId));
            }

            int squad = DefaultInitialSquad;
            string weaponId = DefaultWeaponId;
            float rewardMultiplier = DefaultRewardMultiplier;
            int bonusAbilityCharges = 0;

            if (activeBoosts != null)
            {
                foreach (string boost in activeBoosts)
                {
                    if (string.Equals(boost, "squad_plus_5", StringComparison.OrdinalIgnoreCase))
                    {
                        squad += 5;
                    }
                    else if (string.Equals(boost, "squad_plus_10", StringComparison.OrdinalIgnoreCase))
                    {
                        squad += 10;
                    }
                    else if (string.Equals(boost, "double_rewards", StringComparison.OrdinalIgnoreCase))
                    {
                        rewardMultiplier *= 2.0f;
                    }
                    else if (string.Equals(boost, "bonus_charge", StringComparison.OrdinalIgnoreCase))
                    {
                        bonusAbilityCharges += 1;
                    }
                }
            }

            var mutationList = selectedMutations != null ? new List<string>(selectedMutations) : new List<string>();

            return new RunConfig(stageId, squad, weaponId, rewardMultiplier, mutationList, bonusAbilityCharges);
        }
    }
}
