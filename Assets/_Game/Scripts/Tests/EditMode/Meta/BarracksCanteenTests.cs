using System;
using Game.Core;
using Game.Meta.Barracks;
using Game.Meta.Save;
using Game.Meta.Time;
using Game.Meta.Wallet;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    [TestFixture]
    public class BarracksCanteenTests
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
        public void CT_09_Canteen_GeneratesCoinsProportionalToOfflineTime()
        {
            _timeProvider.Advance(TimeSpan.FromHours(4));
            int pending = _service.GetPendingOfflineCoins();

            Assert.AreEqual(200, pending);
        }

        [Test]
        public void CT_10_Canteen_OfflineTimeExceeding8Hours_CapsAt8Hours_AndSubsequentCollectionYieldsZero()
        {
            _timeProvider.Advance(TimeSpan.FromHours(24));
            int pending = _service.GetPendingOfflineCoins();
            Assert.AreEqual(400, pending);

            bool collected = _service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(400, coins);
            Assert.AreEqual(400, _wallet.GetBalance(CurrencyKind.Coins));

            bool immediateSecondCollection = _service.TryCollectOfflineCoins(false, out int secondCoins);
            Assert.IsFalse(immediateSecondCollection);
            Assert.AreEqual(0, secondCoins);
        }

        [Test]
        public void CT_11_Canteen_CollectWithRewardedAd_DoublesCreditedCoins()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));

            bool collected = _service.TryCollectOfflineCoins(true, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(200, coins);
            Assert.AreEqual(200, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void CT_12_AntiCheat_WhenClockManipulated_FreezesProduction()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            Assert.AreEqual(100, _service.GetPendingOfflineCoins());

            _timeProvider.Advance(TimeSpan.FromHours(-3));
            Assert.IsTrue(_timeProvider.IsClockManipulated);
            Assert.IsTrue(_service.IsClockManipulated);

            Assert.AreEqual(0, _service.GetPendingOfflineCoins());
            bool collected = _service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsFalse(collected);
            Assert.AreEqual(0, coins);
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void ADV_02_Canteen_CollectionWithZeroIntegerCoins_ReturnsFalse_AndPreservesTimestamp()
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(10));

            bool collected = _service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsFalse(collected);
            Assert.AreEqual(0, coins);

            _timeProvider.Advance(TimeSpan.FromSeconds(62));
            Assert.AreEqual(1, _service.GetPendingOfflineCoins());
            Assert.IsTrue(_service.TryCollectOfflineCoins(false, out int collectedCoins));
            Assert.AreEqual(1, collectedCoins);
            Assert.AreEqual(1, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void Canteen_FractionalTimePreserved_OnSuccessfulCollectionUnderCap()
        {
            _timeProvider.Advance(TimeSpan.FromSeconds(100));
            Assert.AreEqual(1, _service.GetPendingOfflineCoins());

            bool firstCollected = _service.TryCollectOfflineCoins(false, out int firstCoins);
            Assert.IsTrue(firstCollected);
            Assert.AreEqual(1, firstCoins);
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());

            _timeProvider.Advance(TimeSpan.FromSeconds(44));
            Assert.AreEqual(1, _service.GetPendingOfflineCoins());

            bool secondCollected = _service.TryCollectOfflineCoins(false, out int secondCoins);
            Assert.IsTrue(secondCollected);
            Assert.AreEqual(1, secondCoins);
            Assert.AreEqual(2, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void Canteen_OfflineBeyond8Hours_SurplusDiscarded_SubsequentTimeAccruesFromUtcNow()
        {
            _timeProvider.Advance(TimeSpan.FromHours(12));
            Assert.AreEqual(400, _service.GetPendingOfflineCoins());

            bool collected = _service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsTrue(collected);
            Assert.AreEqual(400, coins);

            _timeProvider.Advance(TimeSpan.FromHours(1));
            Assert.AreEqual(50, _service.GetPendingOfflineCoins());
        }

        [Test]
        public void Canteen_BuildingLevels_ProduceScaleAppropriateCoins()
        {
            _service.SetBuildingLevel(BarracksBuildingType.Canteen, 2);
            _timeProvider.Advance(TimeSpan.FromHours(2));
            Assert.AreEqual(200, _service.GetPendingOfflineCoins());

            _service.SetBuildingLevel(BarracksBuildingType.Canteen, 3);
            Assert.AreEqual(300, _service.GetPendingOfflineCoins());

            _service.SetBuildingLevel(BarracksBuildingType.Canteen, 5);
            _timeProvider.Advance(TimeSpan.FromHours(10));
            Assert.AreEqual(2000, _service.GetPendingOfflineCoins());
        }

        [Test]
        public void Canteen_ZeroOrNegativeElapsed_ReturnsZeroAndDoesNotCollect()
        {
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());
            bool collected = _service.TryCollectOfflineCoins(false, out int coins);
            Assert.IsFalse(collected);
            Assert.AreEqual(0, coins);
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void Canteen_ClockNormalization_ResumesPendingAccrual()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _timeProvider.Advance(TimeSpan.FromHours(-3));
            Assert.IsTrue(_service.IsClockManipulated);
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());

            _timeProvider.Advance(TimeSpan.FromHours(4));
            Assert.IsFalse(_service.IsClockManipulated);
            Assert.That(_service.GetPendingOfflineCoins(), Is.GreaterThan(0));
        }
    }
}
