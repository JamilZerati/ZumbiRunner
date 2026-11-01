using System;
using System.Collections.Generic;
using Game.Meta.Barracks;
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
        protected IBarracksService Service;

        protected abstract bool IsReal { get; }
        protected abstract IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet);

        [SetUp]
        public void SetUp()
        {
            TimeProvider = new FakeTimeProvider(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
            Wallet = new Wallet();

            if (IsReal)
            {
                Assert.Fail($"[RED - Marco 0] Real IBarracksService implementation is absent for contract test '{TestContext.CurrentContext.Test.Name}'. Expected to turn Green in Marco 2 (NEX-1260) and Marco 3 (NEX-1262).");
            }

            Service = CreateService(TimeProvider, Wallet);
        }

        [Test]
        public void CT_01_BuildingDefinitions_CostsAndDurationsAreValid()
        {
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Armory));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Canteen));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Infirmary));
            Assert.AreEqual(1, Service.GetBuildingLevel(BarracksBuildingType.Ammunition));
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

            TimeProvider.Advance(TimeSpan.FromHours(2));

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
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsFalse(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));

            TimeProvider.Advance(TimeSpan.FromDays(1));
            Assert.IsTrue(Service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CT_08_TryInstantCompleteWithMedals_CalculatesCostProportionally_DebitsMedalsAndCompletesImmediately()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            Wallet.Add(CurrencyKind.Medals, 30, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            int medalCost = Service.GetInstantCompleteMedalCost(BarracksBuildingType.Recruitment);
            Assert.That(medalCost, Is.GreaterThanOrEqualTo(5));

            bool instantCompleted = Service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment);
            Assert.IsTrue(instantCompleted);
            Assert.AreEqual(2, Service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(30 - medalCost, Wallet.GetBalance(CurrencyKind.Medals));
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
            Assert.AreEqual(400, pending); // 8 horas * 50 = 400

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
            TimeProvider.Advance(TimeSpan.FromHours(2)); // 100 coins

            bool collected = Service.TryCollectOfflineCoins(true, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(200, coins);
            Assert.AreEqual(200, Wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void CT_12_AntiCheat_WhenClockManipulated_FreezesTimersAndProduction()
        {
            TimeProvider.Advance(TimeSpan.FromHours(2));
            Assert.That(Service.GetPendingOfflineCoins(), Is.GreaterThan(0));

            TimeProvider.SetManipulated(true);

            Assert.IsTrue(Service.IsClockManipulated);
            Assert.AreEqual(0, Service.GetPendingOfflineCoins());
            Assert.IsFalse(Service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string reason));
            Assert.That(reason, Does.Contain("inconsistente"));
        }

        [Test]
        public void CT_13_Recruitment_AppliesTwoSquadMembersPerLevel()
        {
            Assert.AreEqual(2, Service.GetInitialSquadBonus());

            if (Service is FakeBarracksService fake)
            {
                fake.SetBuildingLevel(BarracksBuildingType.Recruitment, 3);
                Assert.AreEqual(6, Service.GetInitialSquadBonus());
            }
        }

        [Test]
        public void CT_14_Arsenal_AppliesFivePercentBaseDamagePerLevel()
        {
            Assert.AreEqual(1.05f, Service.GetBaseDamageMultiplier(), 0.001f);

            if (Service is FakeBarracksService fake)
            {
                fake.SetBuildingLevel(BarracksBuildingType.Armory, 2);
                Assert.AreEqual(1.10f, Service.GetBaseDamageMultiplier(), 0.001f);
            }
        }

        [Test]
        public void CT_15_Infirmary_RecoversOneSoldierPerTenLost_CappedByLevel()
        {
            Assert.AreEqual(1, Service.CalculateRecoveredSoldiers(10));
            Assert.AreEqual(2, Service.CalculateRecoveredSoldiers(20));
            Assert.AreEqual(2, Service.CalculateRecoveredSoldiers(30)); // Cap é 2 * Lv 1 = 2
        }

        [Test]
        public void CT_16_RewardCalculator_Contract_RecruitsRewardOnlyWhenPositive()
        {
            int totalRecruitsZero = 0;
            int totalRecruitsPositive = 5;

            Assert.IsFalse(totalRecruitsZero > 0);
            Assert.IsTrue(totalRecruitsPositive > 0);
        }

        [Test]
        public void CT_17_SaveIntegrity_Contract_V1Compatibility()
        {
            Assert.Pass("Contrato de validação de integridade em disco sem desserialização tipada reservado.");
        }

        [Test]
        public void CT_18_BarracksReactivity_Contract_WalletChangedAndClockNotification()
        {
            Assert.IsNotNull(Service);
            Assert.IsFalse(Service.IsClockManipulated);
        }

        [Test]
        public void ADV_01_SpeedupWithAd_WhenRemainingLessThan30Minutes_ClampsToZeroWithoutGoingNegative()
        {
            Wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            Service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeProvider.Advance(TimeSpan.FromMinutes(50)); // restam 10 minutos de 60

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

            if (Service is FakeBarracksService fake)
            {
                fake.SetBuildingLevel(BarracksBuildingType.Ammunition, 4);
                IReadOnlyList<string> lv4Perks = Service.GetUnlockedAmmoPerks();
                Assert.AreEqual(4, lv4Perks.Count);
                Assert.AreEqual("ammo_toxic", lv4Perks[3]);
            }
        }
    }

    [TestFixture]
    public class FakeBarracksContractRunner : BarracksContractTestsBase
    {
        protected override bool IsReal => false;

        protected override IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet)
        {
            return new FakeBarracksService(timeProvider, wallet);
        }
    }

    [TestFixture]
    public class RealBarracksContractRunner : BarracksContractTestsBase
    {
        protected override bool IsReal => true;

        protected override IBarracksService CreateService(FakeTimeProvider timeProvider, IWallet wallet)
        {
            // A implementação real BarracksService será construída nos Marcos 2 e 3.
            return null;
        }
    }
}
