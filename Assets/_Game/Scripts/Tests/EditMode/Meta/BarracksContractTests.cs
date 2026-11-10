using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Core;
using Game.Core.State;
using Game.Meta.Barracks;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Time;
using Game.Meta.Wallet;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public abstract class BarracksContractTestsBase
    {
        protected FakeTimeProvider TimeProvider;
        protected Wallet Wallet;
        protected EventBus EventBus;
        protected IBarracksService Service;

        protected abstract IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet);
        protected virtual void SetBuildingLevel(BarracksBuildingType type, int level) { }

        [SetUp]
        public void SetUp()
        {
            EventBus = new EventBus();
            TimeProvider = new FakeTimeProvider(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
            Wallet = new Wallet(EventBus);

            Service = CreateService(TimeProvider, Wallet);
            string testName = TestContext.CurrentContext.Test.Name;
            if (testName != nameof(CT_16_RewardCalculator_Contract_RecruitsRewardOnlyWhenPositive) &&
                testName != nameof(CT_17_SaveIntegrity_Contract_V1Compatibility) &&
                Service == null)
            {
                Assert.Fail($"[RED - Marco 0] Real IBarracksService implementation is absent for contract test '{testName}'. Expected to turn Green in Marco 2 (NEX-1260) and Marco 3 (NEX-1262).");
            }
        }

        [Test]
        public void CT_01_BuildingDefinitions_CostsAndDurationsAreValid()
        {
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Armory));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Canteen));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Infirmary));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Ammunition));

            Assert.That(Service.GetUpgradeCoinCost(BarracksBuildingType.Recruitment), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeRecruitCost(BarracksBuildingType.Recruitment), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeDuration(BarracksBuildingType.Recruitment), Is.GreaterThan(TimeSpan.Zero));

            Assert.That(Service.GetUpgradeCoinCost(BarracksBuildingType.Armory), Is.GreaterThan(0));
            Assert.AreEqual(0, Service.GetUpgradeRecruitCost(BarracksBuildingType.Armory));
            Assert.That(Service.GetUpgradeDuration(BarracksBuildingType.Armory), Is.GreaterThan(TimeSpan.Zero));

            Assert.That(Service.GetUpgradeCoinCost(BarracksBuildingType.Canteen), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeRecruitCost(BarracksBuildingType.Canteen), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeDuration(BarracksBuildingType.Canteen), Is.GreaterThan(TimeSpan.Zero));

            Assert.That(Service.GetUpgradeCoinCost(BarracksBuildingType.Infirmary), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeRecruitCost(BarracksBuildingType.Infirmary), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeDuration(BarracksBuildingType.Infirmary), Is.GreaterThan(TimeSpan.Zero));

            Assert.AreEqual(0, Service.GetUpgradeCoinCost(BarracksBuildingType.Ammunition));
            Assert.That(Service.GetUpgradeRecruitCost(BarracksBuildingType.Ammunition), Is.GreaterThan(0));
            Assert.That(Service.GetUpgradeDuration(BarracksBuildingType.Ammunition), Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public void CT_02_TryStartUpgrade_WhenCoinsSufficientButRecruitsInsufficient_RejectsAndDoesNotDebitCoins()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Assert.AreEqual(0, Wallet.GetBalance(CurrencyKind.Recruits));

            bool started = Service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsFalse(started);
            Assert.AreEqual(500, Wallet.GetBalance(CurrencyKind.Coins));
            Assert.IsFalse(Service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_03_TryStartUpgrade_WhenConstructionAlreadyActive_RejectsSecondConstruction()
        {
            Wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool firstStarted = Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(firstStarted);

            bool secondStarted = Service.TryStartUpgrade(BarracksBuildingType.Armory);
            Assert.IsFalse(secondStarted);
        }

        [Test]
        public void CT_04_TryCompleteUpgrade_WhenTimeElapsed_SucceedsAndIncrementsLevel()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool started = Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(started);

            TimeProvider.Advance(TimeSpan.FromHours(5));

            bool completed = Service.TryCompleteUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(completed);
            Assert.AreEqual(2, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsFalse(Service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_05_TryCompleteUpgrade_WhenPremature_FailsAndLevelRemainsUnchanged()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool started = Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(started);

            bool completed = Service.TryCompleteUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsFalse(completed);
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_06_TrySpeedupWithAd_ReducesConstructionTimeByExactly30Minutes()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeSpan initialRemaining = Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);

            bool spedUp = Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);
            Assert.IsTrue(spedUp);

            TimeSpan afterSpeedup = Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining.TotalSeconds - 1800, afterSpeedup.TotalSeconds, 1.0);
        }

        [Test]
        public void CT_07_TrySpeedupWithAd_FourthAttemptOnSameDay_RejectedByDailyLimit_AndResetsNextDay()
        {
            Wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsFalse(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));

            TimeProvider.Advance(TimeSpan.FromHours(5));
            Assert.IsTrue(Service.TryCompleteUpgrade(BarracksBuildingType.Recruitment));

            TimeProvider.Advance(TimeSpan.FromDays(1));

            Assert.IsTrue(Service.TryStartUpgrade(BarracksBuildingType.Armory));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Armory));
        }

        [Test]
        public void CT_08_TryInstantCompleteWithMedals_CalculatesCostProportionally_DebitsMedalsAndCompletesImmediately()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            Wallet.Add(CurrencyKind.Medals, 60, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            int medalCost = Service.GetInstantCompleteMedalCost(BarracksBuildingType.Recruitment);
            Assert.AreEqual(48, medalCost);

            bool instantCompleted = Service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment);
            Assert.IsTrue(instantCompleted);
            Assert.AreEqual(2, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(60 - medalCost, Wallet.GetBalance(CurrencyKind.Medals));
            Assert.IsFalse(Service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_09_Canteen_GeneratesCoinsProportionalToOfflineTime()
        {
            TimeProvider.Advance(TimeSpan.FromHours(4));
            int pending = Service.GetPendingOfflineCoins();

            Assert.AreEqual(200, pending);
        }

        [Test]
        public void CT_10_Canteen_OfflineTimeExceeding8Hours_CapsAt8Hours_AndSubsequentCollectionYieldsZero()
        {
            TimeProvider.Advance(TimeSpan.FromHours(24));
            int pending = Service.GetPendingOfflineCoins();
            Assert.AreEqual(400, pending);

            bool collected = Service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(400, coins);
            Assert.AreEqual(400, Wallet.GetBalance(CurrencyKind.Coins));

            bool immediateSecondCollection = Service.TryCollectOfflineCoins(false, out int secondCoins);
            Assert.IsFalse(immediateSecondCollection);
            Assert.AreEqual(0, secondCoins);
        }

        [Test]
        public void CT_11_Canteen_CollectWithRewardedAd_DoublesCreditedCoins()
        {
            TimeProvider.Advance(TimeSpan.FromHours(2));

            bool collected = Service.TryCollectOfflineCoins(true, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(200, coins);
            Assert.AreEqual(200, Wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void CT_12_AntiCheat_WhenClockManipulated_FreezesTimersAndProduction()
        {
            Wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool started = Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(started);
            TimeSpan initialRemaining = Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.That(initialRemaining, Is.GreaterThan(TimeSpan.Zero));

            TimeProvider.Advance(TimeSpan.FromHours(2));
            Assert.That(Service.GetPendingOfflineCoins(), Is.GreaterThan(0));

            TimeProvider.Advance(TimeSpan.FromHours(-3));
            Assert.IsTrue(TimeProvider.IsClockManipulated);
            Assert.IsTrue(Service.IsClockManipulated);

            Assert.AreEqual(0, Service.GetPendingOfflineCoins());

            Assert.IsFalse(Service.CanStartUpgrade(BarracksBuildingType.Armory, out string reason));
            Assert.That(reason, Does.Contain("inconsistente"));

            TimeSpan frozenRemaining = Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining.TotalSeconds - TimeSpan.FromHours(2).TotalSeconds, frozenRemaining.TotalSeconds, 1.0);
            Assert.IsFalse(Service.TryCompleteUpgrade(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_13_Recruitment_AppliesTwoSquadMembersPerLevel()
        {
            Assert.AreEqual(2, Service.GetInitialSquadBonus());

            SetBuildingLevel(BarracksBuildingType.Recruitment, 3);
            Assert.AreEqual(6, Service.GetInitialSquadBonus());
        }

        [Test]
        public void CT_14_Arsenal_AppliesFivePercentBaseDamagePerLevel()
        {
            Assert.AreEqual(1.05f, Service.GetBaseDamageMultiplier(), 0.001f);

            SetBuildingLevel(BarracksBuildingType.Armory, 2);
            Assert.AreEqual(1.10f, Service.GetBaseDamageMultiplier(), 0.001f);
        }

        [Test]
        public void CT_15_Infirmary_RecoversOneSoldierPerTenLost_CappedByLevel()
        {
            Assert.AreEqual(1, Service.CalculateRecoveredSoldiers(10));
            Assert.AreEqual(2, Service.CalculateRecoveredSoldiers(20));
            Assert.AreEqual(2, Service.CalculateRecoveredSoldiers(30));
        }

        [Test]
        public void CT_16_RewardCalculator_Contract_RecruitsRewardOnlyWhenPositive()
        {
            var calculator = new RewardCalculator();
            var runResultZeroRecruits = new RunResult { LevelId = "level_01", CoinsEarned = 100, Rescued = 0 };
            var bundle = calculator.Calculate(runResultZeroRecruits, null);

            // Contrato: Nenhuma recompensa de Recruits deve existir quando Rescued + RecoveredSoldiers == 0
            bool hasRecruitsReward = false;
            foreach (var reward in bundle.Rewards)
            {
                if (reward is CurrencyReward cr && cr.Currency == CurrencyKind.Recruits)
                {
                    hasRecruitsReward = true;
                    Assert.That(cr.Amount, Is.GreaterThan(0), "CurrencyReward de recrutas nunca pode ter valor <= 0.");
                }
            }

            Assert.IsFalse(hasRecruitsReward, "Nenhum CurrencyReward de recrutas deve ser gerado quando o total for zero.");

            // Blindagem de carteira: Wallet.Add(amount <= 0) deve obrigatoriamente lançar ArgumentOutOfRangeException
            var testWallet = new Wallet();
            Assert.Throws<ArgumentOutOfRangeException>(() => testWallet.Add(CurrencyKind.Recruits, 0, "ZeroGrant"));
        }

        [Test]
        public void CT_17_SaveIntegrity_Contract_V1Compatibility()
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"save_v1_contract_{Guid.NewGuid():N}.json");
            string staticTempPath = Path.Combine(Path.GetTempPath(), $"save_v1_static_{Guid.NewGuid():N}.json");
            try
            {
                var store = new SaveStore(null, null, currentVersion: 1);
                var v1State = new MetaState
                {
                    SaveVersion = 1,
                    LastSeenUtcTicks = 638000000000000000L,
                    Currencies = new Dictionary<string, int> { { "coins", 1000 }, { "medals", 10 } }
                };
                store.Save(v1State, tempPath);

                Assert.IsTrue(store.ValidateIntegrity(tempPath), "Save V1 com hash original deve validar integridade.");

                string json = File.ReadAllText(tempPath);
                Assert.That(json, Does.Contain("\"SaveVersion\": 1"));
                Assert.That(json, Does.Contain("\"IntegrityHash\":"));

                File.WriteAllText(staticTempPath, json, Encoding.UTF8);
                Assert.IsTrue(store.ValidateIntegrity(staticTempPath), "Save V1 estático autêntico sem chave BarracksState deve validar integridade.");
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                if (File.Exists(staticTempPath))
                {
                    File.Delete(staticTempPath);
                }
            }
        }

        [Test]
        public void CT_18_BarracksReactivity_Contract_WalletChangedAndClockNotification()
        {
            var receivedEvents = new List<WalletChangedEvent>();
            EventBus.Subscribe<WalletChangedEvent>(e => receivedEvents.Add(e));

            Assert.IsFalse(Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string coinReason));
            Assert.That(coinReason, Does.Contain("Moedas insuficientes"));

            Wallet.Add(CurrencyKind.Coins, 300, "TestCoinReward");
            Assert.AreEqual(1, receivedEvents.Count);
            Assert.AreEqual(CurrencyKind.Coins, receivedEvents[0].Currency);

            Assert.IsFalse(Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string recruitReason));
            Assert.That(recruitReason, Does.Contain("Recrutas insuficientes"));

            Wallet.Add(CurrencyKind.Recruits, 20, "TestRecruitsReward");
            Assert.AreEqual(2, receivedEvents.Count);
            Assert.AreEqual(CurrencyKind.Recruits, receivedEvents[1].Currency);

            Assert.IsTrue(Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out _));

            TimeProvider.SetManipulated(true);
            Assert.IsTrue(Service.IsClockManipulated);
            Assert.IsFalse(Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string clockReason));
            Assert.That(clockReason, Does.Contain("inconsistente"));
        }

        [Test]
        public void ADV_01_SpeedupWithAd_WhenRemainingLessThan30Minutes_ClampsToZeroWithoutGoingNegative()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeSpan duration = Service.GetUpgradeDuration(BarracksBuildingType.Recruitment);
            TimeProvider.Advance(duration - TimeSpan.FromMinutes(10));

            bool spedUp = Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);
            Assert.IsTrue(spedUp);
            Assert.AreEqual(TimeSpan.Zero, Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void ADV_02_Canteen_CollectionWithZeroIntegerCoins_ReturnsFalse_AndPreservesTimestamp()
        {
            TimeProvider.Advance(TimeSpan.FromSeconds(10));

            bool collected = Service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsFalse(collected);
            Assert.AreEqual(0, coins);

            TimeProvider.Advance(TimeSpan.FromSeconds(62));
            Assert.AreEqual(1, Service.GetPendingOfflineCoins());
            Assert.IsTrue(Service.TryCollectOfflineCoins(false, out int collectedCoins));
            Assert.AreEqual(1, collectedCoins);
        }

        [Test]
        public void ADV_03_InstantCompleteWithMedals_WhenMedalsInsufficient_FailsAndDoesNotComplete()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            Assert.AreEqual(0, Wallet.GetBalance(CurrencyKind.Medals));

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            bool instantCompleted = Service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment);
            Assert.IsFalse(instantCompleted);
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void ADV_04_Infirmary_WhenLossesLessThanTen_RecoversZeroSoldiers()
        {
            Assert.AreEqual(0, Service.CalculateRecoveredSoldiers(9));
            Assert.AreEqual(0, Service.CalculateRecoveredSoldiers(0));
        }

        [Test]
        public void ADV_05_AmmunitionWorkshop_UnlocksPerksSequentiallyByLevel()
        {
            IReadOnlyList<string> lv1Perks = Service.GetUnlockedAmmoPerks();
            Assert.AreEqual(1, lv1Perks.Count);
            Assert.AreEqual("ammo_fire", lv1Perks[0]);

            SetBuildingLevel(BarracksBuildingType.Ammunition, 4);
            IReadOnlyList<string> lv4Perks = Service.GetUnlockedAmmoPerks();
            Assert.AreEqual(4, lv4Perks.Count);
            Assert.AreEqual("ammo_toxic", lv4Perks[3]);
        }

        [Test]
        public void ADV_06_CanStartUpgrade_WhenLevelAtMaxLevel5_ReturnsFalseWithMaxLevelReason()
        {
            SetBuildingLevel(BarracksBuildingType.Recruitment, 5);
            bool canStart = Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string reason);
            Assert.IsFalse(canStart);
            Assert.That(reason, Does.Contain("nível máximo"));
        }

        [Test]
        public void ADV_07_CalculateRecoveredSoldiers_WhenSoldiersLostNegative_ReturnsZero()
        {
            Assert.AreEqual(0, Service.CalculateRecoveredSoldiers(-10));
            Assert.AreEqual(0, Service.CalculateRecoveredSoldiers(-1));
        }

        [Test]
        public void ADV_08_TrySpeedupWithAd_WhenConstructionAlreadyExpired_ReturnsFalse()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeProvider.Advance(TimeSpan.FromHours(5));

            Assert.AreEqual(TimeSpan.Zero, Service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment));
            bool spedUp = Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);
            Assert.IsFalse(spedUp);
        }
    }

    [TestFixture]
    public class FakeBarracksContractRunner : BarracksContractTestsBase
    {
        protected override IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet)
        {
            return new FakeBarracksService(timeProvider, wallet);
        }

        protected override void SetBuildingLevel(BarracksBuildingType type, int level)
        {
            ((FakeBarracksService)Service).SetBuildingLevel(type, level);
        }
    }

    [TestFixture]
    public class RealBarracksContractRunner : BarracksContractTestsBase
    {
        protected override IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet)
        {
            return new BarracksService(timeProvider, wallet, null, null, EventBus);
        }

        protected override void SetBuildingLevel(BarracksBuildingType type, int level)
        {
            ((BarracksService)Service).SetBuildingLevel(type, level);
        }
    }
}
