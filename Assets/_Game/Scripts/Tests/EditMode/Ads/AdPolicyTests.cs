using System;
using System.Collections.Generic;
using Game.Core.Ads;
using Game.Meta.Ads;
using Game.Meta.Tracks;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;

namespace Game.Tests.EditMode.Ads
{
    [TestFixture]
    public class AdPolicyTests
    {
        private FakeTimeProvider _timeProvider;
        private Entitlements _entitlements;
        private AdPolicyDefinition _definition;
        private AdPolicyState _state;
        private AdPolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
            _entitlements = new Entitlements();
            _definition = new AdPolicyDefinition();
            _state = new AdPolicyState();
            _policy = new AdPolicy(_timeProvider, _entitlements, _definition, _state);
        }

        [Test]
        public void CT_03_CanShowInterstitial_WhenPlayerOnStageBelow5_ReturnsFalse()
        {
            _state.StagesCompletedSinceLastInterstitial = 5;
            _state.StagesCompletedInCurrentSession = 3;
            _state.LastAdShownUtcTicks = 0;
            _state.LastRewardedOrPurchaseUtcTicks = 0;

            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 4));
            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 1));
        }

        [Test]
        public void CT_04_CanShowInterstitial_WhenPlayerOnStage5_After3StagesAnd91Seconds_ReturnsTrue()
        {
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.StagesCompletedInCurrentSession = 2;
            _state.LastAdShownUtcTicks = _timeProvider.UtcNow.AddSeconds(-91).Ticks;
            _state.LastRewardedOrPurchaseUtcTicks = _timeProvider.UtcNow.AddSeconds(-150).Ticks;

            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CT_05_CanShowInterstitial_WhenAdShown60SecondsAgo_ReturnsFalse()
        {
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.StagesCompletedInCurrentSession = 2;
            _state.LastAdShownUtcTicks = _timeProvider.UtcNow.AddSeconds(-60).Ticks;
            _state.LastRewardedOrPurchaseUtcTicks = 0;

            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));

            _timeProvider.Advance(TimeSpan.FromSeconds(31));
            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CT_06_CanShowInterstitial_WhenRewardedAdShown100SecondsAgo_ReturnsFalse()
        {
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.StagesCompletedInCurrentSession = 2;
            _state.LastAdShownUtcTicks = _timeProvider.UtcNow.AddSeconds(-100).Ticks;
            _state.LastRewardedOrPurchaseUtcTicks = _timeProvider.UtcNow.AddSeconds(-100).Ticks;

            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));

            _timeProvider.Advance(TimeSpan.FromSeconds(21));
            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CT_06_CanShowInterstitial_WhenPurchaseMade100SecondsAgo_ReturnsFalse()
        {
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.StagesCompletedInCurrentSession = 2;
            _state.LastAdShownUtcTicks = 0;
            _policy.RecordPurchase();

            _timeProvider.Advance(TimeSpan.FromSeconds(100));
            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));

            _timeProvider.Advance(TimeSpan.FromSeconds(21));
            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CT_07_CanShowInterstitial_FirstSessionOfDay_RequiresAtLeast2StagesInSession()
        {
            _policy.RecordSessionStart();
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.LastAdShownUtcTicks = 0;
            _state.LastRewardedOrPurchaseUtcTicks = 0;

            Assert.AreEqual(0, _state.StagesCompletedInCurrentSession);
            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));

            _policy.RecordStageCompleted();
            Assert.AreEqual(1, _state.StagesCompletedInCurrentSession);
            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));

            _policy.RecordStageCompleted();
            Assert.AreEqual(2, _state.StagesCompletedInCurrentSession);
            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CanShowInterstitial_SecondSessionOfDay_DoesNotRequireStage2Gate()
        {
            _policy.RecordSessionStart();
            _policy.RecordSessionStart();
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.LastAdShownUtcTicks = 0;
            _state.LastRewardedOrPurchaseUtcTicks = 0;

            Assert.AreEqual(2, _state.SessionsStartedInCurrentDay);
            Assert.AreEqual(0, _state.StagesCompletedInCurrentSession);
            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
        }

        [Test]
        public void CT_08_EntitlementNoAds_BlocksInterstitial_PreservesRewarded()
        {
            _state.StagesCompletedSinceLastInterstitial = 3;
            _state.StagesCompletedInCurrentSession = 2;
            _state.LastAdShownUtcTicks = 0;
            _state.LastRewardedOrPurchaseUtcTicks = 0;

            Assert.IsTrue(_policy.CanShowInterstitial(playerHighestStage: 5));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.DoubleReward));

            _entitlements.GrantEntitlement("no_ads");

            Assert.IsFalse(_policy.CanShowInterstitial(playerHighestStage: 5));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.DoubleReward));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.Revive));
        }

        [Test]
        public void CT_09_CanShowRewarded_WhenDailyLimitReached_ReturnsFalse_ResetsOnUtcDayChange()
        {
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.DoubleReward));
                _policy.RecordAdShown(AdPlacement.DoubleReward, AdType.Rewarded);
            }

            Assert.AreEqual(20, _state.DailyRewardedCount);
            Assert.IsFalse(_policy.CanShowRewarded(AdPlacement.DoubleReward));
            Assert.IsFalse(_policy.CanShowRewarded(AdPlacement.Revive));

            _timeProvider.Advance(TimeSpan.FromHours(13));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.DoubleReward));
            Assert.AreEqual(0, _state.DailyRewardedCount);
        }

        [Test]
        public void CanShowRewarded_BarracksSpeedup_RestrictedByPlacementLimit_ResetsOnUtcDayChange()
        {
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.BarracksSpeedup));
                _policy.RecordAdShown(AdPlacement.BarracksSpeedup, AdType.Rewarded);
            }

            Assert.IsFalse(_policy.CanShowRewarded(AdPlacement.BarracksSpeedup));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.DoubleReward));

            _timeProvider.Advance(TimeSpan.FromDays(1));
            Assert.IsTrue(_policy.CanShowRewarded(AdPlacement.BarracksSpeedup));
        }

        [Test]
        public void RecordAdShown_Interstitial_ResetsStagesSinceLastInterstitial_AndUpdatesTicks()
        {
            _state.StagesCompletedSinceLastInterstitial = 5;
            _policy.RecordAdShown(AdPlacement.InterLevel, AdType.Interstitial);

            Assert.AreEqual(0, _state.StagesCompletedSinceLastInterstitial);
            Assert.AreEqual(_timeProvider.UtcNow.Ticks, _state.LastInterstitialUtcTicks);
            Assert.AreEqual(_timeProvider.UtcNow.Ticks, _state.LastAdShownUtcTicks);
        }
    }
}
