using System;
using System.Collections.Generic;
using Game.Composition;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Infrastructure.Ads;
using Game.Meta.Ads;
using Game.Meta.Stages;
using Game.Meta.Time;
using Game.Meta.Tracks;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Ads
{
    [TestFixture]
    public class LevelPlayAndCompositionAdTests
    {
        private sealed class FakeTimeProvider : ITimeProvider
        {
            public DateTime CurrentUtc { get; set; } = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => CurrentUtc;
            public long LastSeenUtcTicks => CurrentUtc.Ticks;
            public bool IsClockManipulated => false;
            public void RecordCurrentTime() { }
            public void AdvanceSeconds(double seconds) => CurrentUtc = CurrentUtc.AddSeconds(seconds);
        }

        [Test]
        public void LevelPlayAdService_DefaultsToFallbackMock_WhenSdkNotAvailable()
        {
            var service = new LevelPlayAdService();

            Assert.IsTrue(service.IsUsingFallback);
            Assert.IsNotNull(service.FallbackService);
            Assert.IsTrue(service.IsAdAvailable(AdPlacement.InterLevel));
        }

        [Test]
        public void LevelPlayAdService_DelegatesAvailabilityAndShowAd_ToFallback()
        {
            var mock = new MockAdService();
            mock.SetPlacementAvailable(AdPlacement.InterLevel, false);
            var service = new LevelPlayAdService(fallbackService: mock);

            Assert.IsFalse(service.IsAdAvailable(AdPlacement.InterLevel));

            mock.SetPlacementAvailable(AdPlacement.InterLevel, true);
            Assert.IsTrue(service.IsAdAvailable(AdPlacement.InterLevel));

            var completed = false;
            service.ShowAd(AdPlacement.InterLevel, result =>
            {
                completed = true;
                Assert.AreEqual(AdShowResult.Completed, result);
            });
            Assert.IsTrue(completed);
        }

        [Test]
        public void LevelPlayAdService_Initialize_SetsAppKeyAndInitializedFlag()
        {
            var service = new LevelPlayAdService();
            Assert.IsFalse(service.IsInitialized);

            service.Initialize("test_app_key_123");

            Assert.IsTrue(service.IsInitialized);
            Assert.AreEqual("test_app_key_123", service.AppKey);
            Assert.IsTrue(service.IsUsingFallback);
        }

        [Test]
        public void GreyboxRunComposer_DefaultsToLevelPlayAndAdPolicy_WhenComposed()
        {
            var go = new GameObject("ComposerDefaultTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();

            try
            {
                composer.Compose(bus);

                Assert.IsNotNull(composer.AdService);
                Assert.IsNotNull(composer.AdPolicy);
                Assert.IsNotNull(composer.TimeProvider);
                Assert.IsNotNull(composer.Entitlements);
                Assert.IsInstanceOf<LevelPlayAdService>(composer.AdService);
                Assert.IsInstanceOf<AdPolicy>(composer.AdPolicy);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_TryShowInterLevelInterstitial_WhenStageBelow5_ReturnsFalse()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.SetPlayerHighestStage(4);
                composer.Compose(bus);

                policy.State.StagesCompletedSinceLastInterstitial = 5;
                policy.State.StagesCompletedInCurrentSession = 5;

                bool shown = composer.TryShowInterLevelInterstitial();

                Assert.IsFalse(shown);
                Assert.AreEqual(0, mockAds.ShowAdCallCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_TryShowInterLevelInterstitial_WhenIntervalBelow3_ReturnsFalse()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.SetPlayerHighestStage(5);
                composer.Compose(bus);

                policy.State.StagesCompletedSinceLastInterstitial = 2;
                policy.State.StagesCompletedInCurrentSession = 3;

                bool shown = composer.TryShowInterLevelInterstitial();

                Assert.IsFalse(shown);
                Assert.AreEqual(0, mockAds.ShowAdCallCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_TryShowInterLevelInterstitial_WhenConditionsMet_ShowsAdAndUpdatesCooldown()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.SetPlayerHighestStage(5);
                composer.Compose(bus);

                policy.State.StagesCompletedSinceLastInterstitial = 3;
                policy.State.StagesCompletedInCurrentSession = 3;
                policy.State.LastAdShownUtcTicks = 0;
                policy.State.LastRewardedOrPurchaseUtcTicks = 0;

                bool callbackInvoked = false;
                bool shown = composer.TryShowInterLevelInterstitial(() => callbackInvoked = true);

                Assert.IsTrue(shown);
                Assert.IsTrue(callbackInvoked);
                Assert.AreEqual(1, mockAds.ShowAdCallCount);
                Assert.AreEqual(0, policy.State.StagesCompletedSinceLastInterstitial);
                Assert.AreEqual(time.UtcNow.Ticks, policy.State.LastInterstitialUtcTicks);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_TryShowInterLevelInterstitial_WhenNoAdsEntitlement_SuppressesAd()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            entitlements.GrantEntitlement("no_ads");
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.SetPlayerHighestStage(5);
                composer.Compose(bus);

                policy.State.StagesCompletedSinceLastInterstitial = 5;
                policy.State.StagesCompletedInCurrentSession = 5;

                bool shown = composer.TryShowInterLevelInterstitial();

                Assert.IsFalse(shown);
                Assert.AreEqual(0, mockAds.ShowAdCallCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_OnGameStateChanged_FromResultToHub_TriggersInterstitialPacing()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.SetPlayerHighestStage(6);
                composer.Compose(bus);

                policy.State.StagesCompletedSinceLastInterstitial = 4;
                policy.State.StagesCompletedInCurrentSession = 4;

                bus.Publish(new GameStateChangedEvent(GameState.Result, GameState.Hub));

                Assert.AreEqual(1, mockAds.ShowAdCallCount);
                Assert.AreEqual(0, policy.State.StagesCompletedSinceLastInterstitial);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void GreyboxRunComposer_OnRunEnded_IncrementsStagesCompleted()
        {
            var go = new GameObject("ComposerTest");
            var composer = go.AddComponent<GreyboxRunComposer>();
            var bus = new EventBus();
            var time = new FakeTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);
            var mockAds = new MockAdService();

            try
            {
                composer.SetAdServices(mockAds, policy, time, entitlements);
                composer.Compose(bus);

                int beforeInterval = policy.State.StagesCompletedSinceLastInterstitial;
                int beforeSession = policy.State.StagesCompletedInCurrentSession;

                bus.Publish(new RunEndedEvent(new RunResult { Victory = true }));

                Assert.AreEqual(beforeInterval + 1, policy.State.StagesCompletedSinceLastInterstitial);
                Assert.AreEqual(beforeSession + 1, policy.State.StagesCompletedInCurrentSession);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
