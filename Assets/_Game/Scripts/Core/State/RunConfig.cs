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
        public float BaseDamageMultiplier { get; set; } = 1.0f;
        public int InfirmaryLevel { get; set; } = 0;
        public IReadOnlyList<string> UnlockedAmmoPerks { get; set; }

        public RunConfig()
        {
            InitialSquad = 10;
            RewardMultiplier = 1f;
            MutationIds = new List<string>();
            BaseDamageMultiplier = 1.0f;
            InfirmaryLevel = 0;
            UnlockedAmmoPerks = new List<string>();
        }

        public RunConfig(string levelId, int initialSquad, string weaponId, float rewardMultiplier, IReadOnlyList<string> mutationIds, int bonusAbilityCharges = 0, float baseDamageMultiplier = 1.0f, int infirmaryLevel = 0, IReadOnlyList<string> unlockedAmmoPerks = null)
        {
            LevelId = levelId;
            InitialSquad = initialSquad;
            WeaponId = weaponId;
            RewardMultiplier = rewardMultiplier;
            MutationIds = mutationIds ?? new List<string>();
            BonusAbilityCharges = bonusAbilityCharges;
            BaseDamageMultiplier = baseDamageMultiplier > 0f ? baseDamageMultiplier : 1.0f;
            InfirmaryLevel = infirmaryLevel >= 0 ? infirmaryLevel : 0;
            UnlockedAmmoPerks = unlockedAmmoPerks ?? new List<string>();
        }
    }
}
