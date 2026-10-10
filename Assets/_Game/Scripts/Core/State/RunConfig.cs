using System.Collections.Generic;

namespace Game.Core.State
{
    public class RunConfig
    {
        public string LevelId { get; set; }
        public int InitialSquad { get; set; }
        public string WeaponId { get; set; }
        public float RewardMultiplier { get; set; }
        public IReadOnlyList<string> MutationIds { get; set; }
        public int BonusAbilityCharges { get; set; }

        public RunConfig()
        {
            InitialSquad = 10;
            RewardMultiplier = 1f;
            MutationIds = new List<string>();
        }

        public RunConfig(string levelId, int initialSquad, string weaponId, float rewardMultiplier, IReadOnlyList<string> mutationIds, int bonusAbilityCharges = 0)
        {
            LevelId = levelId;
            InitialSquad = initialSquad;
            WeaponId = weaponId;
            RewardMultiplier = rewardMultiplier;
            MutationIds = mutationIds ?? new List<string>();
            BonusAbilityCharges = bonusAbilityCharges;
        }
    }
}
