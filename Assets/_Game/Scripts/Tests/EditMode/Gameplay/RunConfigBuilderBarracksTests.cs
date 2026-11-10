using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Barracks;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Time;
using Game.Meta.Wallet;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;

namespace Game.Tests.EditMode.Gameplay
{
    [TestFixture]
    public class RunConfigBuilderBarracksTests
    {
        [Test]
        public void CT_13_RunConfigBuilder_AppliesTwoSquadMembersPerRecruitmentLevel()
        {
            var metaState = new MetaState();
            metaState.Barracks.SetLevel("Recruitment", 3);

            var builder = new RunConfigBuilder(metaState);
            RunConfig config = builder.Build("stage_01");

            // CT-13: Lv 3 = 16 soldados (10 base + 3 * 2 = 16)
            Assert.AreEqual(16, config.InitialSquad);
        }

        [Test]
        public void CT_13_RunConfigBuilder_DefaultRecruitmentLevel1_GivesTwelveSquad()
        {
            var metaState = new MetaState();
            metaState.Barracks.SetLevel("Recruitment", 1);

            var builder = new RunConfigBuilder(metaState);
            RunConfig config = builder.Build("stage_01");

            Assert.AreEqual(12, config.InitialSquad);
        }

        [Test]
        public void RunConfigBuilder_WhenMetaStateNull_UsesDefaultInitialSquadTen()
        {
            var builder = new RunConfigBuilder(null);
            RunConfig config = builder.Build("stage_01");

            Assert.AreEqual(10, config.InitialSquad);
            Assert.AreEqual(1.0f, config.BaseDamageMultiplier);
            Assert.AreEqual(0, config.InfirmaryLevel);
            Assert.AreEqual(0, config.UnlockedAmmoPerks.Count);
        }

        [Test]
        public void CT_14_RunConfigBuilder_AppliesFivePercentDamagePerArsenalLevel()
        {
            var metaState = new MetaState();
            metaState.Barracks.SetLevel("Armory", 2);

            var builder = new RunConfigBuilder(metaState);
            RunConfig config = builder.Build("stage_01");

            // CT-14: Lv 2 = 1.10f
            Assert.AreEqual(1.10f, config.BaseDamageMultiplier, 0.001f);
        }

        [Test]
        public void RunConfigBuilder_InjectsInfirmaryLevelAndAmmunitionPerks()
        {
            var metaState = new MetaState();
            metaState.Barracks.SetLevel("Infirmary", 3);
            metaState.Barracks.SetLevel("Ammunition", 2);

            var builder = new RunConfigBuilder(metaState);
            RunConfig config = builder.Build("stage_01");

            Assert.AreEqual(3, config.InfirmaryLevel);
            Assert.AreEqual(2, config.UnlockedAmmoPerks.Count);
            Assert.AreEqual("ammo_fire", config.UnlockedAmmoPerks[0]);
            Assert.AreEqual("ammo_shock", config.UnlockedAmmoPerks[1]);
        }

        [Test]
        public void RunConfigBuilder_WithBarracksService_InjectsAccurateStats()
        {
            var timeProvider = new FakeTimeProvider();
            var wallet = new Wallet();
            var service = new FakeBarracksService(timeProvider, wallet);
            service.SetBuildingLevel(BarracksBuildingType.Recruitment, 4);
            service.SetBuildingLevel(BarracksBuildingType.Armory, 3);
            service.SetBuildingLevel(BarracksBuildingType.Infirmary, 2);
            service.SetBuildingLevel(BarracksBuildingType.Ammunition, 3);

            var builder = new RunConfigBuilder(null, service);
            RunConfig config = builder.Build("stage_test");

            Assert.AreEqual(18, config.InitialSquad); // 10 + 4 * 2 = 18
            Assert.AreEqual(1.15f, config.BaseDamageMultiplier, 0.001f);
            Assert.AreEqual(2, config.InfirmaryLevel);
            Assert.AreEqual(3, config.UnlockedAmmoPerks.Count);
        }

        [Test]
        public void RunConfigBuilder_CombinesBarracksSquadWithActiveBoosts()
        {
            var metaState = new MetaState();
            metaState.Barracks.SetLevel("Recruitment", 3); // 16 soldados

            var builder = new RunConfigBuilder(metaState);
            var boosts = new List<string> { "squad_plus_5", "double_rewards" };
            RunConfig config = builder.Build("stage_01", activeBoosts: boosts);

            Assert.AreEqual(21, config.InitialSquad); // 16 + 5 = 21
            Assert.AreEqual(2.0f, config.RewardMultiplier, 0.001f);
        }
    }
}
