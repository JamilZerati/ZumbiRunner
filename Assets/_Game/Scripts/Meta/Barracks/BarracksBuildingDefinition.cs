using System.Collections.Generic;

namespace Game.Meta.Barracks
{
    public class BarracksBuildingLevelDefinition
    {
        public int Level { get; set; }
        public int CoinCost { get; set; }
        public int RecruitCost { get; set; }
        public int DurationSeconds { get; set; }
        public float EffectValue { get; set; }
        public int CoinsPerHour { get; set; }
        public string UnlockedPerkId { get; set; }
    }

    public class BarracksBuildingDefinition
    {
        public BarracksBuildingType Type { get; set; }
        public string DisplayName { get; set; }
        public int MaxLevel { get; set; }
        public IReadOnlyList<BarracksBuildingLevelDefinition> Levels { get; set; }

        public BarracksBuildingLevelDefinition GetLevel(int level)
        {
            if (Levels == null) return null;
            for (int i = 0; i < Levels.Count; i++)
            {
                if (Levels[i].Level == level)
                {
                    return Levels[i];
                }
            }
            return null;
        }
    }
}
