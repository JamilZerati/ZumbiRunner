using System;
using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Barracks;
using Game.Meta.Save;

namespace Game.Meta.Run
{
    public class RunConfigBuilder : IRunConfigBuilder
    {
        private const int DefaultInitialSquad = 10;
        private const string DefaultWeaponId = "rifle";
        private const float DefaultRewardMultiplier = 1.0f;

        private readonly MetaState _metaState;
        private readonly IBarracksService _barracksService;

        public RunConfigBuilder(MetaState metaState = null, IBarracksService barracksService = null)
        {
            _metaState = metaState;
            _barracksService = barracksService;
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
            float baseDamageMultiplier = 1.0f;
            int infirmaryLevel = 0;
            IReadOnlyList<string> unlockedAmmoPerks = null;

            if (_barracksService != null)
            {
                squad += _barracksService.GetInitialSquadBonus();
                baseDamageMultiplier = _barracksService.GetBaseDamageMultiplier();
                infirmaryLevel = _barracksService.GetBuildingLevel(BarracksBuildingType.Infirmary);
                unlockedAmmoPerks = _barracksService.GetUnlockedAmmoPerks();
            }
            else if (_metaState?.Barracks != null)
            {
                int recruitmentLevel = _metaState.Barracks.GetLevel("Recruitment");
                squad += 2 * recruitmentLevel;

                int armoryLevel = _metaState.Barracks.GetLevel("Armory");
                baseDamageMultiplier = 1.0f + 0.05f * armoryLevel;

                infirmaryLevel = _metaState.Barracks.GetLevel("Infirmary");

                int ammoLevel = _metaState.Barracks.GetLevel("Ammunition");
                string[] perkCatalog = { "ammo_fire", "ammo_shock", "ammo_cryo", "ammo_toxic" };
                var ammoList = new List<string>();
                for (int i = 0; i < ammoLevel && i < perkCatalog.Length; i++)
                {
                    ammoList.Add(perkCatalog[i]);
                }
                unlockedAmmoPerks = ammoList;
            }

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

            return new RunConfig(stageId, squad, weaponId, rewardMultiplier, mutationList, bonusAbilityCharges, baseDamageMultiplier, infirmaryLevel, unlockedAmmoPerks);
        }
    }
}
