using System.Linq;
using Game.Meta.Barracks;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class BarracksBuildingDefinitionTests
    {
        private BarracksCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = BarracksCatalog.CreateDefault();
        }

        [Test]
        public void CreateDefault_ContainsAllFiveCanonicalBuildings()
        {
            var buildings = _catalog.GetAll();
            Assert.AreEqual(5, buildings.Count);

            Assert.IsNotNull(_catalog.Get(BarracksBuildingType.Recruitment));
            Assert.IsNotNull(_catalog.Get(BarracksBuildingType.Armory));
            Assert.IsNotNull(_catalog.Get(BarracksBuildingType.Canteen));
            Assert.IsNotNull(_catalog.Get(BarracksBuildingType.Infirmary));
            Assert.IsNotNull(_catalog.Get(BarracksBuildingType.Ammunition));
        }

        [Test]
        public void RecruitmentDefinition_HasCorrectParametersAcrossAllLevels()
        {
            var def = _catalog.Get(BarracksBuildingType.Recruitment);
            Assert.AreEqual("Recrutamento", def.DisplayName);
            Assert.AreEqual(5, def.MaxLevel);
            Assert.AreEqual(5, def.Levels.Count);

            for (int lv = 1; lv <= 5; lv++)
            {
                var levelDef = def.GetLevel(lv);
                Assert.IsNotNull(levelDef);
                Assert.AreEqual(lv, levelDef.Level);
                Assert.AreEqual(100 * lv, levelDef.CoinCost);
                Assert.AreEqual(5 * lv, levelDef.RecruitCost);
                Assert.AreEqual(14400, levelDef.DurationSeconds);
                Assert.AreEqual(2f * lv, levelDef.EffectValue);
                Assert.AreEqual(0, levelDef.CoinsPerHour);
                Assert.IsNull(levelDef.UnlockedPerkId);
            }
        }

        [Test]
        public void ArmoryDefinition_HasCorrectParametersAcrossAllLevels()
        {
            var def = _catalog.Get(BarracksBuildingType.Armory);
            Assert.AreEqual("Arsenal", def.DisplayName);
            Assert.AreEqual(5, def.MaxLevel);
            Assert.AreEqual(5, def.Levels.Count);

            for (int lv = 1; lv <= 5; lv++)
            {
                var levelDef = def.GetLevel(lv);
                Assert.IsNotNull(levelDef);
                Assert.AreEqual(lv, levelDef.Level);
                Assert.AreEqual(150 * lv, levelDef.CoinCost);
                Assert.AreEqual(0, levelDef.RecruitCost);
                Assert.AreEqual(14400, levelDef.DurationSeconds);
                Assert.AreEqual(0.05f * lv, levelDef.EffectValue, 0.0001f);
                Assert.AreEqual(0, levelDef.CoinsPerHour);
                Assert.IsNull(levelDef.UnlockedPerkId);
            }
        }

        [Test]
        public void CanteenDefinition_HasCorrectParametersAcrossAllLevels()
        {
            var def = _catalog.Get(BarracksBuildingType.Canteen);
            Assert.AreEqual("Refeitório", def.DisplayName);
            Assert.AreEqual(5, def.MaxLevel);
            Assert.AreEqual(5, def.Levels.Count);

            for (int lv = 1; lv <= 5; lv++)
            {
                var levelDef = def.GetLevel(lv);
                Assert.IsNotNull(levelDef);
                Assert.AreEqual(lv, levelDef.Level);
                Assert.AreEqual(80 * lv, levelDef.CoinCost);
                Assert.AreEqual(3 * lv, levelDef.RecruitCost);
                Assert.AreEqual(14400, levelDef.DurationSeconds);
                Assert.AreEqual(50 * lv, levelDef.CoinsPerHour);
                Assert.IsNull(levelDef.UnlockedPerkId);
            }
        }

        [Test]
        public void InfirmaryDefinition_HasCorrectParametersAcrossAllLevels()
        {
            var def = _catalog.Get(BarracksBuildingType.Infirmary);
            Assert.AreEqual("Enfermaria", def.DisplayName);
            Assert.AreEqual(5, def.MaxLevel);
            Assert.AreEqual(5, def.Levels.Count);

            for (int lv = 1; lv <= 5; lv++)
            {
                var levelDef = def.GetLevel(lv);
                Assert.IsNotNull(levelDef);
                Assert.AreEqual(lv, levelDef.Level);
                Assert.AreEqual(120 * lv, levelDef.CoinCost);
                Assert.AreEqual(5 * lv, levelDef.RecruitCost);
                Assert.AreEqual(14400, levelDef.DurationSeconds);
                Assert.AreEqual(2f * lv, levelDef.EffectValue);
                Assert.AreEqual(0, levelDef.CoinsPerHour);
                Assert.IsNull(levelDef.UnlockedPerkId);
            }
        }

        [Test]
        public void AmmunitionDefinition_UnlocksPerksSequentially()
        {
            var def = _catalog.Get(BarracksBuildingType.Ammunition);
            Assert.AreEqual("Oficina de Munição", def.DisplayName);
            Assert.AreEqual(5, def.MaxLevel);
            Assert.AreEqual(5, def.Levels.Count);

            string[] expectedPerks = { "ammo_fire", "ammo_shock", "ammo_cryo", "ammo_toxic", null };

            for (int lv = 1; lv <= 5; lv++)
            {
                var levelDef = def.GetLevel(lv);
                Assert.IsNotNull(levelDef);
                Assert.AreEqual(lv, levelDef.Level);
                Assert.AreEqual(0, levelDef.CoinCost);
                Assert.AreEqual(10 * lv, levelDef.RecruitCost);
                Assert.AreEqual(14400, levelDef.DurationSeconds);
                Assert.AreEqual(expectedPerks[lv - 1], levelDef.UnlockedPerkId);
            }
        }

        [Test]
        public void GetLevel_WhenInvalid_ReturnsNull()
        {
            var def = _catalog.Get(BarracksBuildingType.Recruitment);
            Assert.IsNull(def.GetLevel(0));
            Assert.IsNull(def.GetLevel(6));
        }

        [Test]
        public void Get_WhenUnknownType_ReturnsNull()
        {
            Assert.IsNull(_catalog.Get((BarracksBuildingType)999));
        }
    }
}
