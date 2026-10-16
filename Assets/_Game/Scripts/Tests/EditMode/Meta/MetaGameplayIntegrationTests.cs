using System.Collections.Generic;
using Game.Core;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class MetaGameplayIntegrationTests
    {
        [Test]
        public void RunConfigBuilder_AppliesBoosts_AndCustomMutations()
        {
            var builder = new RunConfigBuilder();
            var boosts = new List<string> { "squad_plus_5", "double_rewards", "bonus_charge" };
            var mutations = new List<string> { "fog_of_war", "armored_horde" };

            RunConfig config = builder.Build("stage_03", mutations, boosts);

            Assert.AreEqual("stage_03", config.LevelId);
            Assert.AreEqual(15, config.InitialSquad);
            Assert.AreEqual("rifle", config.WeaponId);
            Assert.AreEqual(2.0f, config.RewardMultiplier);
            Assert.AreEqual(1, config.BonusAbilityCharges);
            Assert.AreEqual(2, config.MutationIds.Count);
            CollectionAssert.Contains(config.MutationIds, "fog_of_war");
            CollectionAssert.Contains(config.MutationIds, "armored_horde");
        }

        [Test]
        public void RewardCalculator_ComputesCorrectCoins_WithMutationBonuses()
        {
            var mutationBonuses = new Dictionary<string, float>
            {
                { "hard_boss", 0.5f },
                { "fast_runners", 0.25f }
            };
            var calculator = new RewardCalculator(mutationBonuses);

            var config = new RunConfig("stage_01", 10, "rifle", 1.0f, new[] { "hard_boss", "fast_runners" });
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 200,
                MultiplierReached = 2
            };

            RewardBundle bundle = calculator.Calculate(result, config);

            // base 200 * multiplier 2 * (1 + 0.5 + 0.25 = 1.75) = 700
            Assert.IsNotNull(bundle);
            Assert.AreEqual(1, bundle.Rewards.Count);
            var reward = bundle.Rewards[0] as CurrencyReward;
            Assert.IsNotNull(reward);
            Assert.AreEqual(CurrencyKind.Coins, reward.Currency);
            Assert.AreEqual(700, reward.Amount);
        }

        [Test]
        public void RewardCalculator_DoubleReward_DoublesCoins()
        {
            var calculator = new RewardCalculator();
            var config = new RunConfig("stage_01", 10, "rifle", 1.0f, null);
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 150,
                MultiplierReached = 2
            };

            RewardBundle bundleNormal = calculator.Calculate(result, config, doubleReward: false);
            RewardBundle bundleDouble = calculator.Calculate(result, config, doubleReward: true);

            var normalReward = bundleNormal.Rewards[0] as CurrencyReward;
            var doubleReward = bundleDouble.Rewards[0] as CurrencyReward;

            Assert.IsNotNull(normalReward);
            Assert.IsNotNull(doubleReward);
            Assert.AreEqual(300, normalReward.Amount);
            Assert.AreEqual(600, doubleReward.Amount);
        }

        [Test]
        public void FullLifecycle_RunResult_To_RewardCalculator_To_WalletCredit()
        {
            var eventBus = new EventBus();
            var wallet = new Wallet(eventBus);
            var sink = new RewardSink(wallet, null);
            var calculator = new RewardCalculator();

            var config = new RunConfig("stage_01", 10, "rifle", 1.5f, null);
            var result = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                MultiplierReached = 3
            };

            RewardBundle bundle = calculator.Calculate(result, config);
            bool firstGrant = bundle.Grant(sink);

            // 100 * 3 * 1.5 = 450
            Assert.IsTrue(firstGrant);
            Assert.AreEqual(450, wallet.GetBalance(CurrencyKind.Coins));

            // Re-granting bundle must be idempotent
            bool secondGrant = bundle.Grant(sink);
            Assert.IsFalse(secondGrant);
            Assert.AreEqual(450, wallet.GetBalance(CurrencyKind.Coins));
        }
    }
}
