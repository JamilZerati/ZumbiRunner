using System.Collections.Generic;

namespace Game.Meta.Barracks
{
    public class BarracksCatalog : IBarracksCatalog
    {
        private readonly Dictionary<BarracksBuildingType, BarracksBuildingDefinition> _definitions;

        public BarracksCatalog(IEnumerable<BarracksBuildingDefinition> definitions)
        {
            _definitions = new Dictionary<BarracksBuildingType, BarracksBuildingDefinition>();
            if (definitions != null)
            {
                foreach (var def in definitions)
                {
                    _definitions[def.Type] = def;
                }
            }
        }

        public BarracksBuildingDefinition Get(BarracksBuildingType type)
        {
            return _definitions.TryGetValue(type, out var def) ? def : null;
        }

        public IReadOnlyCollection<BarracksBuildingDefinition> GetAll()
        {
            return _definitions.Values;
        }

        public static BarracksCatalog CreateDefault()
        {
            var definitions = new List<BarracksBuildingDefinition>
            {
                CreateRecruitmentDefinition(),
                CreateArmoryDefinition(),
                CreateCanteenDefinition(),
                CreateInfirmaryDefinition(),
                CreateAmmunitionDefinition()
            };

            return new BarracksCatalog(definitions);
        }

        private static BarracksBuildingDefinition CreateRecruitmentDefinition()
        {
            const int maxLevel = 5;
            var levels = new List<BarracksBuildingLevelDefinition>(maxLevel);
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                levels.Add(new BarracksBuildingLevelDefinition
                {
                    Level = lv,
                    CoinCost = 100 * lv,
                    RecruitCost = 5 * lv,
                    DurationSeconds = 14400,
                    EffectValue = 2f * lv,
                    CoinsPerHour = 0,
                    UnlockedPerkId = null
                });
            }

            return new BarracksBuildingDefinition
            {
                Type = BarracksBuildingType.Recruitment,
                DisplayName = "Recrutamento",
                MaxLevel = maxLevel,
                Levels = levels
            };
        }

        private static BarracksBuildingDefinition CreateArmoryDefinition()
        {
            const int maxLevel = 5;
            var levels = new List<BarracksBuildingLevelDefinition>(maxLevel);
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                levels.Add(new BarracksBuildingLevelDefinition
                {
                    Level = lv,
                    CoinCost = 150 * lv,
                    RecruitCost = 0,
                    DurationSeconds = 14400,
                    EffectValue = 0.05f * lv,
                    CoinsPerHour = 0,
                    UnlockedPerkId = null
                });
            }

            return new BarracksBuildingDefinition
            {
                Type = BarracksBuildingType.Armory,
                DisplayName = "Arsenal",
                MaxLevel = maxLevel,
                Levels = levels
            };
        }

        private static BarracksBuildingDefinition CreateCanteenDefinition()
        {
            const int maxLevel = 5;
            var levels = new List<BarracksBuildingLevelDefinition>(maxLevel);
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                levels.Add(new BarracksBuildingLevelDefinition
                {
                    Level = lv,
                    CoinCost = 80 * lv,
                    RecruitCost = 3 * lv,
                    DurationSeconds = 14400,
                    EffectValue = 50f * lv,
                    CoinsPerHour = 50 * lv,
                    UnlockedPerkId = null
                });
            }

            return new BarracksBuildingDefinition
            {
                Type = BarracksBuildingType.Canteen,
                DisplayName = "Refeitório",
                MaxLevel = maxLevel,
                Levels = levels
            };
        }

        private static BarracksBuildingDefinition CreateInfirmaryDefinition()
        {
            const int maxLevel = 5;
            var levels = new List<BarracksBuildingLevelDefinition>(maxLevel);
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                levels.Add(new BarracksBuildingLevelDefinition
                {
                    Level = lv,
                    CoinCost = 120 * lv,
                    RecruitCost = 5 * lv,
                    DurationSeconds = 14400,
                    EffectValue = 2f * lv,
                    CoinsPerHour = 0,
                    UnlockedPerkId = null
                });
            }

            return new BarracksBuildingDefinition
            {
                Type = BarracksBuildingType.Infirmary,
                DisplayName = "Enfermaria",
                MaxLevel = maxLevel,
                Levels = levels
            };
        }

        private static BarracksBuildingDefinition CreateAmmunitionDefinition()
        {
            const int maxLevel = 5;
            string[] perks = { "ammo_fire", "ammo_shock", "ammo_cryo", "ammo_toxic", null };
            var levels = new List<BarracksBuildingLevelDefinition>(maxLevel);
            for (int lv = 1; lv <= maxLevel; lv++)
            {
                levels.Add(new BarracksBuildingLevelDefinition
                {
                    Level = lv,
                    CoinCost = 0,
                    RecruitCost = 10 * lv,
                    DurationSeconds = 14400,
                    EffectValue = lv,
                    CoinsPerHour = 0,
                    UnlockedPerkId = perks[lv - 1]
                });
            }

            return new BarracksBuildingDefinition
            {
                Type = BarracksBuildingType.Ammunition,
                DisplayName = "Oficina de Munição",
                MaxLevel = maxLevel,
                Levels = levels
            };
        }
    }
}
