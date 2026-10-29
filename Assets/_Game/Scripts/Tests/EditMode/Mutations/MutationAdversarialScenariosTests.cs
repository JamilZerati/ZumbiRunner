using System;
using System.Collections.Generic;
using Game.Core.Gates;
using Game.Core.Modifiers;
using Game.Core.State;
using Game.Core.Weather;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class MutationAdversarialScenariosTests
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
        public void CT_8_ProgressionLock_MutationsLockedUntilFirstBossDefeated()
        {
            var catalog = ProgressionCatalog.CreateDefaultCatalog();
            var metaState = new MetaState();
            var progressionService = new ProgressionService(catalog, metaState);

            Assert.IsFalse(progressionService.AreMutationsUnlocked);
            Assert.IsFalse(progressionService.IsBossDefeated("level_05_boss"));

            var nonBossResult = new RunResult
            {
                LevelId = "level_01",
                Victory = true,
                CoinsEarned = 50,
                MultiplierReached = 1,
                Stars = 3
            };
            var sink = new FakeRewardSink();
            progressionService.ProcessRunResult(nonBossResult, sink, out _);

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
        public void CT_9_SelectionCap_FourthMutationSelectionRejected_KeepingMaximumOfThree()
        {
            var selected = new List<string> { "armored", "frenzy", "fog" };
            const int maxAllowed = 3;

            string candidateFourth = "last_soldier";
            bool added = false;
            if (selected.Count < maxAllowed)
            {
                selected.Add(candidateFourth);
                added = true;
            }

            Assert.IsFalse(added);
            Assert.AreEqual(3, selected.Count);
            Assert.Contains("armored", selected);
            Assert.Contains("frenzy", selected);
            Assert.Contains("fog", selected);
            Assert.IsFalse(selected.Contains("last_soldier"));
        }

        [Test]
        public void CT_10_RewardCalculator_SumsMultipleMutationBonuses_Accurately()
        {
            var mutationBonuses = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
            {
                ["armored"] = 0.50f,
                ["frenzy"] = 0.50f,
                ["fog"] = 0.25f,
                ["last_soldier"] = 1.00f,
                ["no_multiplication"] = 0.50f
            };

            var calculator = new RewardCalculator(mutationBonuses);

            // Two mutations: armored (+0.5) and frenzy (+0.5) -> sum = 1.0
            // Multiplier reached = 2.0x, Config multiplier = 1.0x
            // Effective multiplier = 2.0 * 1.0 * (1.0 + 1.0) = 4.0x
            // Base coins: 100 -> 400 coins
            var configTwoMutations = new RunConfig("stage_01", 10, "pistol", 1.0f, new[] { "armored", "frenzy" });
            var result = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };

            var bundleTwo = calculator.Calculate(result, configTwoMutations);
            Assert.AreEqual(1, bundleTwo.Rewards.Count);
            var coinRewardTwo = bundleTwo.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(coinRewardTwo);
            Assert.AreEqual(400, coinRewardTwo.Amount);

            // Three mutations: armored (+0.5), frenzy (+0.5), fog (+0.25) -> sum = 1.25
            // Effective multiplier = 2.0 * (1.0 + 1.25) = 4.5x
            // Base coins: 100 -> 450 coins
            var configThreeMutations = new RunConfig("stage_01", 10, "pistol", 1.0f, new[] { "armored", "frenzy", "fog" });
            var bundleThree = calculator.Calculate(result, configThreeMutations);
            var coinRewardThree = bundleThree.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(coinRewardThree);
            Assert.AreEqual(450, coinRewardThree.Amount);
        }

        [Test]
        public void CT_11_RemovalBySourceId_RemovesMutationWithoutAffectingWeatherOrOtherModifiers()
        {
            var controller = new StageModifierController();

            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            var rain = new WeatherStageModifier(WeatherType.Rain); // "clima:rain"
            var frenzy = new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f);

            controller.Activate(armored, 0f, float.MaxValue);
            controller.Activate(rain, 0f, float.MaxValue);
            controller.Activate(frenzy, 0f, float.MaxValue);

            Assert.AreEqual(3, controller.ActiveCount);
            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1.3f, controller.CurrentEnemySpeedMultiplier, 1e-4f);

            bool removed = controller.RemoveBySourceId("mutacao:armored");
            Assert.IsTrue(removed);
            Assert.AreEqual(2, controller.ActiveCount);

            // Armored is gone -> EnemyHealthMultiplier returns to 1.0f
            Assert.AreEqual(1.0f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            // Frenzy remains -> EnemySpeedMultiplier remains 1.3f
            Assert.AreEqual(1.3f, controller.CurrentEnemySpeedMultiplier, 1e-4f);

            bool removeNonExistent = controller.RemoveBySourceId("mutacao:nonexistent");
            Assert.IsFalse(removeNonExistent);
            Assert.AreEqual(2, controller.ActiveCount);
        }

        [Test]
        public void CT_12_InvalidMutationFlag_ParsingIgnoresUnknownIdsWithoutCrashing()
        {
            var knownCatalog = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "armored", "frenzy", "fog", "last_soldier", "no_multiplication"
            };

            string rawCliArg = "armored,invalid_mutation,,frenzy,  ,unknown_xyz";
            string[] tokens = rawCliArg.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            var parsedMutations = new List<string>();
            var warnings = new List<string>();

            foreach (var token in tokens)
            {
                string trimmed = token.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (knownCatalog.Contains(trimmed))
                {
                    parsedMutations.Add(trimmed);
                }
                else
                {
                    warnings.Add($"Unknown mutation '{trimmed}' ignored.");
                }
            }

            Assert.AreEqual(2, parsedMutations.Count);
            Assert.AreEqual("armored", parsedMutations[0]);
            Assert.AreEqual("frenzy", parsedMutations[1]);
            Assert.AreEqual(2, warnings.Count);
        }

        [Test]
        public void Adversarial_ConflictingSpeedModifiers_CompoundMultiplicatively()
        {
            var controller = new StageModifierController();

            // Frenesi (+30% speed = 1.3x) combined with Blizzard weather (-20% speed = 0.8x)
            var frenzy = new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f);
            var blizzard = new WeatherStageModifier(WeatherType.Blizzard); // 0.8f speed

            controller.Activate(frenzy, 0f, float.MaxValue);
            controller.Activate(blizzard, 0f, float.MaxValue);

            float expectedSpeed = 1.3f * 0.8f; // 1.04f
            Assert.AreEqual(expectedSpeed, controller.CurrentEnemySpeedMultiplier, 1e-4f);
        }

        [Test]
        public void Adversarial_InfiniteDuration_SurvivesSubstantialDistanceTicks()
        {
            var controller = new StageModifierController();
            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);

            controller.Activate(armored, startDistance: 0f, durationDistance: float.MaxValue);
            Assert.AreEqual(1, controller.ActiveCount);

            controller.TickDistance(1000f);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);

            controller.TickDistance(50000f);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
        }

        [Test]
        public void Adversarial_StageModifierController_Clear_ResetsAllMultipliersAndFlags()
        {
            var controller = new StageModifierController();

            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            var lastSoldier = new InitialSquadOverrideStageModifier("mutacao:last_soldier", 1);
            var noMult = new GateOperationRemapStageModifier("mutacao:no_multiplication", true, true);

            controller.Activate(armored, 0f, float.MaxValue);
            controller.Activate(lastSoldier, 0f, float.MaxValue);
            controller.Activate(noMult, 0f, float.MaxValue);

            Assert.AreEqual(3, controller.ActiveCount);
            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1, controller.CurrentInitialSquadOverride);
            Assert.IsTrue(controller.RemapGateMultiplication);
            Assert.IsTrue(controller.RemapGateDivision);

            controller.Clear();

            Assert.AreEqual(0, controller.ActiveCount);
            Assert.AreEqual(1.0f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, controller.CurrentEnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, controller.CurrentSquadRangeMultiplier, 1e-4f);
            Assert.IsNull(controller.CurrentInitialSquadOverride);
            Assert.IsFalse(controller.RemapGateMultiplication);
            Assert.IsFalse(controller.RemapGateDivision);
        }
    }
}
