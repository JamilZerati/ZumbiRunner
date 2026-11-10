using System;
using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class MutationMetaRewardTests
    {
        private class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);

            public IWallet Wallet => null;
            public IEntitlements Entitlements { get; } = new Entitlements();

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) => _unlocked.Add(contentId);
            public bool IsContentUnlocked(string contentId) => _unlocked.Contains(contentId);
        }

        [Test]
        public void RunConfigBuilder_ForcesSquadToOne_WhenLastSoldierMutationPresent()
        {
            var builder = new RunConfigBuilder();
            var mutations = new List<string> { "last_soldier" };

            var config = builder.Build("stage_01", mutations);

            Assert.AreEqual(1, config.InitialSquad);
            Assert.AreEqual(1, config.MutationIds.Count);
            Assert.Contains("last_soldier", (System.Collections.ICollection)config.MutationIds);
        }

        [Test]
        public void RunConfigBuilder_ForcesSquadToOne_OverridingActiveSquadBoosts()
        {
            var builder = new RunConfigBuilder();
            var boosts = new List<string> { "squad_plus_5", "squad_plus_10" };
            var mutations = new List<string> { "armored", "last_soldier", "frenzy" };

            var config = builder.Build("stage_01", mutations, boosts);

            Assert.AreEqual(1, config.InitialSquad);
            Assert.AreEqual(3, config.MutationIds.Count);
        }

        [Test]
        public void RunConfigBuilder_PreservesSquadBoosts_WhenLastSoldierNotPresent()
        {
            var builder = new RunConfigBuilder();
            var boosts = new List<string> { "squad_plus_5", "squad_plus_10" };
            var mutations = new List<string> { "armored", "frenzy" };

            var config = builder.Build("stage_01", mutations, boosts);

            Assert.AreEqual(25, config.InitialSquad);
            Assert.AreEqual(2, config.MutationIds.Count);
        }

        [Test]
        public void ProgressionService_AreMutationsUnlocked_LockedInitially_AndUnlockedAfterLevel05BossDefeated()
        {
            var catalog = ProgressionCatalog.CreateDefaultCatalog();
            var metaState = new MetaState();
            var progressionService = new ProgressionService(catalog, metaState);
            var sink = new FakeRewardSink();

            Assert.IsFalse(progressionService.AreMutationsUnlocked);
            Assert.IsFalse(progressionService.IsBossDefeated("level_05_boss"));

            var normalResult = new RunResult
            {
                LevelId = "level_01",
                Victory = true,
                CoinsEarned = 50,
                MultiplierReached = 1,
                Stars = 3
            };
            progressionService.ProcessRunResult(normalResult, sink, out _);

            Assert.IsFalse(progressionService.AreMutationsUnlocked);

            var bossResult = new RunResult
            {
                LevelId = "level_05_boss",
                Victory = true,
                CoinsEarned = 200,
                MultiplierReached = 2,
                Stars = 3
            };
            bool processed = progressionService.ProcessRunResult(bossResult, sink, out var reward);

            Assert.IsTrue(processed);
            Assert.IsNotNull(reward);
            Assert.IsTrue(progressionService.IsBossDefeated("level_05_boss"));
            Assert.IsTrue(progressionService.AreMutationsUnlocked);
        }

        [Test]
        public void ProgressionService_AreMutationsUnlocked_TrueWhenLoadedFromClaimedState()
        {
            var catalog = ProgressionCatalog.CreateDefaultCatalog();
            var metaState = new MetaState
            {
                ClaimedGrantIds = new HashSet<string> { "unlock_boss_level_05_boss_weapon:shotgun" }
            };
            var progressionService = new ProgressionService(catalog, metaState);

            Assert.IsTrue(progressionService.IsBossDefeated("level_05_boss"));
            Assert.IsTrue(progressionService.AreMutationsUnlocked);
        }

        [Test]
        public void RewardCalculator_SumsMultipleMutationBonuses_Accurately()
        {
            var mutationBonuses = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["armored"] = 0.50f,
                ["frenzy"] = 0.50f,
                ["fog"] = 0.25f,
                ["last_soldier"] = 1.00f
            };

            var calculator = new RewardCalculator(mutationBonuses);
            var config = new RunConfig("stage_01", 10, "rifle", 1.0f, new[] { "armored", "frenzy" });
            var result = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };

            var bundle = calculator.Calculate(result, config, doubleReward: false);
            Assert.AreEqual(1, bundle.Rewards.Count);
            var coinReward = bundle.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(coinReward);
            Assert.AreEqual(400, coinReward.Amount);
        }

        [Test]
        public void RewardCalculator_DoublesRewardCorrectly_WithActiveMutations()
        {
            var mutationBonuses = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["armored"] = 0.50f,
                ["frenzy"] = 0.50f
            };

            var calculator = new RewardCalculator(mutationBonuses);
            var config = new RunConfig("stage_01", 10, "rifle", 1.0f, new[] { "armored", "frenzy" });
            var result = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };

            var bundle = calculator.Calculate(result, config, doubleReward: true);
            Assert.AreEqual(1, bundle.Rewards.Count);
            var coinReward = bundle.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(coinReward);
            Assert.AreEqual(800, coinReward.Amount);
        }

        [Test]
        public void RewardCalculator_HandlesMissingOrEmptyMutationsGracefully()
        {
            var mutationBonuses = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["armored"] = 0.50f
            };

            var calculator = new RewardCalculator(mutationBonuses);
            var config = new RunConfig("stage_01", 10, "rifle", 1.5f, new[] { "unknown_mutation" });
            var result = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 1,
                Stars = 2
            };

            var bundle = calculator.Calculate(result, config, doubleReward: false);
            var coinReward = bundle.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(coinReward);
            Assert.AreEqual(150, coinReward.Amount);
        }
    }
}
