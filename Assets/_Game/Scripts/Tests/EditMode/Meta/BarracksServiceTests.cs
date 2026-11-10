using System;
using System.Collections.Generic;
using Game.Core;
using Game.Meta.Barracks;
using Game.Meta.Barracks.Events;
using Game.Meta.Save;
using Game.Meta.Time;
using Game.Meta.Wallet;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    [TestFixture]
    public class BarracksServiceTests
    {
        private FakeTimeProvider _timeProvider;
        private Wallet _wallet;
        private EventBus _eventBus;
        private BarracksCatalog _catalog;
        private BarracksState _state;
        private BarracksService _service;

        [SetUp]
        public void SetUp()
        {
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
            _eventBus = new EventBus();
            _wallet = new Wallet(_eventBus);
            _catalog = BarracksCatalog.CreateDefault();
            _state = new BarracksState();
            _service = new BarracksService(_timeProvider, _wallet, _catalog, _state, _eventBus);
        }

        [Test]
        public void InitialState_AllBuildingsLevel1_AndNotUnderConstruction()
        {
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Armory));
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Canteen));
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Infirmary));
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Ammunition));

            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            Assert.AreEqual(TimeSpan.Zero, _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void StartUpgrade_WhenCoinsSufficientButRecruitsInsufficient_RejectsAndDoesNotDebitCoins()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Recruits));

            bool started = _service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsFalse(started);
            Assert.AreEqual(500, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void StartUpgrade_WhenRecruitsSufficientButCoinsInsufficient_RejectsAndDoesNotDebitRecruits()
        {
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Coins));

            bool started = _service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsFalse(started);
            Assert.AreEqual(50, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void StartUpgrade_WhenBalancesSufficient_DebitsBothAndStartsConstruction()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            int expectedCoinCost = _service.GetUpgradeCoinCost(BarracksBuildingType.Recruitment);
            int expectedRecruitCost = _service.GetUpgradeRecruitCost(BarracksBuildingType.Recruitment);
            TimeSpan expectedDuration = _service.GetUpgradeDuration(BarracksBuildingType.Recruitment);

            Assert.That(expectedCoinCost, Is.GreaterThan(0));
            Assert.That(expectedRecruitCost, Is.GreaterThan(0));

            bool started = _service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(started);
            Assert.AreEqual(500 - expectedCoinCost, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(50 - expectedRecruitCost, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.IsTrue(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            Assert.AreEqual(expectedDuration.TotalSeconds, _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment).TotalSeconds, 1.0);
        }

        [Test]
        public void StartUpgrade_WhenConstructionAlreadyActive_RejectsSecondConstruction()
        {
            _wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool firstStarted = _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(firstStarted);

            bool secondStarted = _service.TryStartUpgrade(BarracksBuildingType.Armory);
            Assert.IsFalse(secondStarted);
            Assert.IsFalse(_service.CanStartUpgrade(BarracksBuildingType.Armory, out string reason));
            Assert.That(reason, Does.Contain("obra em andamento"));
        }

        [Test]
        public void CompleteUpgrade_WhenTimeElapsed_IncrementsBuildingLevelAndResetsConstruction()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeSpan duration = _service.GetUpgradeDuration(BarracksBuildingType.Recruitment);

            _timeProvider.Advance(duration + TimeSpan.FromMinutes(1));

            bool completed = _service.TryCompleteUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(completed);
            Assert.AreEqual(2, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CompleteUpgrade_WhenPremature_ReturnsFalseAndKeepsConstruction()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            _timeProvider.Advance(TimeSpan.FromHours(1));

            bool completed = _service.TryCompleteUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsFalse(completed);
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsTrue(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void CompleteUpgrade_PublishesBuildingUpgradeCompletedEvent()
        {
            BuildingUpgradeCompletedEvent receivedEvent = default;
            bool eventFired = false;
            _eventBus.Subscribe<BuildingUpgradeCompletedEvent>(e =>
            {
                receivedEvent = e;
                eventFired = true;
            });

            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            _timeProvider.Advance(TimeSpan.FromHours(5));

            _service.TryCompleteUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(BarracksBuildingType.Recruitment, receivedEvent.BuildingType);
            Assert.AreEqual(2, receivedEvent.NewLevel);
        }

        [Test]
        public void SpeedupWithAd_ReducesConstructionTimeBy30Minutes()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeSpan initialRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);

            bool spedUp = _service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);

            Assert.IsTrue(spedUp);
            TimeSpan afterSpeedup = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining.TotalSeconds - 1800, afterSpeedup.TotalSeconds, 1.0);
        }

        [Test]
        public void SpeedupWithAd_ClampsToZeroWhenRemainingLessThan30Minutes()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            TimeSpan duration = _service.GetUpgradeDuration(BarracksBuildingType.Recruitment);
            _timeProvider.Advance(duration - TimeSpan.FromMinutes(10));

            bool spedUp = _service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);

            Assert.IsTrue(spedUp);
            Assert.AreEqual(TimeSpan.Zero, _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void SpeedupWithAd_FourthAttemptOnSameDay_RejectedByDailyLimit_AndResetsNextDay()
        {
            _wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            Assert.IsTrue(_service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(_service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsTrue(_service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsFalse(_service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));

            _timeProvider.Advance(TimeSpan.FromHours(5));
            Assert.IsTrue(_service.TryCompleteUpgrade(BarracksBuildingType.Recruitment));

            _timeProvider.Advance(TimeSpan.FromDays(1));

            Assert.IsTrue(_service.TryStartUpgrade(BarracksBuildingType.Armory));
            Assert.IsTrue(_service.TrySpeedupWithAd(BarracksBuildingType.Armory));
        }

        [Test]
        public void SpeedupWithAd_WhenConstructionExpired_ReturnsFalse()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            _timeProvider.Advance(TimeSpan.FromHours(5));

            Assert.AreEqual(TimeSpan.Zero, _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment));
            bool spedUp = _service.TrySpeedupWithAd(BarracksBuildingType.Recruitment);
            Assert.IsFalse(spedUp);
        }

        [Test]
        public void InstantCompleteWithMedals_CalculatesCostProportionally_DebitsMedalsAndCompletesImmediately()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            _wallet.Add(CurrencyKind.Medals, 60, "InitialBalance");

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            int medalCost = _service.GetInstantCompleteMedalCost(BarracksBuildingType.Recruitment);
            Assert.That(medalCost, Is.GreaterThanOrEqualTo(5));

            bool completed = _service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment);

            Assert.IsTrue(completed);
            Assert.AreEqual(2, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(60 - medalCost, _wallet.GetBalance(CurrencyKind.Medals));
            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void InstantCompleteWithMedals_WhenMedalsInsufficient_FailsAndDoesNotComplete()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Medals));

            _service.TryStartUpgrade(BarracksBuildingType.Recruitment);

            bool completed = _service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment);

            Assert.IsFalse(completed);
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.IsTrue(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
        }

        [Test]
        public void ClockManipulated_FreezesTimersAndRejectsUpgradeOperations()
        {
            _wallet.Add(CurrencyKind.Coins, 1000, "InitialBalance");
            _wallet.Add(CurrencyKind.Recruits, 50, "InitialBalance");

            bool started = _service.TryStartUpgrade(BarracksBuildingType.Recruitment);
            Assert.IsTrue(started);
            TimeSpan initialRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);

            _timeProvider.Advance(TimeSpan.FromHours(2));
            _timeProvider.Advance(TimeSpan.FromHours(-3));

            Assert.IsTrue(_timeProvider.IsClockManipulated);
            Assert.IsTrue(_service.IsClockManipulated);

            Assert.IsFalse(_service.CanStartUpgrade(BarracksBuildingType.Armory, out string reason));
            Assert.That(reason, Does.Contain("inconsistente"));

            Assert.IsFalse(_service.TrySpeedupWithAd(BarracksBuildingType.Recruitment));
            Assert.IsFalse(_service.TryInstantCompleteWithMedals(BarracksBuildingType.Recruitment));
            Assert.IsFalse(_service.TryCompleteUpgrade(BarracksBuildingType.Recruitment));

            TimeSpan frozenRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining.TotalSeconds - TimeSpan.FromHours(2).TotalSeconds, frozenRemaining.TotalSeconds, 1.0);
        }

        [Test]
        public void MaxLevel5_CanStartUpgradeReturnsFalseWithMaxLevelReason()
        {
            _service.SetBuildingLevel(BarracksBuildingType.Recruitment, 5);
            bool canStart = _service.CanStartUpgrade(BarracksBuildingType.Recruitment, out string reason);

            Assert.IsFalse(canStart);
            Assert.That(reason, Does.Contain("nível máximo"));
        }

        [Test]
        public void StatePersistence_InitializesFromExistingBarracksState()
        {
            var savedState = new BarracksState
            {
                BuildingLevels = new Dictionary<string, int>
                {
                    ["Recruitment"] = 3,
                    ["Armory"] = 2,
                    ["Canteen"] = 4,
                    ["Infirmary"] = 1,
                    ["Ammunition"] = 5
                }
            };

            var service = new BarracksService(_timeProvider, _wallet, _catalog, savedState);

            Assert.AreEqual(3, service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual(2, service.GetBuildingLevel(BarracksBuildingType.Armory));
            Assert.AreEqual(4, service.GetBuildingLevel(BarracksBuildingType.Canteen));
            Assert.AreEqual(1, service.GetBuildingLevel(BarracksBuildingType.Infirmary));
            Assert.AreEqual(5, service.GetBuildingLevel(BarracksBuildingType.Ammunition));
        }
    }
}
