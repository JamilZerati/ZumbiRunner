using System.Collections.Generic;
using Game.Core.Ads;
using Game.Core.Analytics;
using NUnit.Framework;

namespace Game.Tests.EditMode.Ads
{
    [TestFixture]
    public class MockAdServiceTests
    {
        private MockAnalyticsService _analytics;
        private MockAdService _adService;

        [SetUp]
        public void SetUp()
        {
            _analytics = new MockAnalyticsService();
            _adService = new MockAdService(_analytics);
        }

        [Test]
        public void CT_01_ShowAd_Completed_InvokesCallbackAndTracksShownAndRewardEvents()
        {
            var callbackInvoked = false;
            var receivedResult = AdShowResult.NotAvailable;

            _adService.ShowAd(AdPlacement.DoubleReward, result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(AdShowResult.Completed, receivedResult);

            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdShown));
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdReward));
            Assert.IsFalse(_analytics.HasEvent(AnalyticsEvents.AdFailed));

            var shownEvents = _analytics.GetEvents(AnalyticsEvents.AdShown);
            Assert.AreEqual(1, shownEvents.Count);
            Assert.AreEqual(AdPlacement.DoubleReward.ToString(), shownEvents[0].GetParameter<string>("placement"));
            Assert.AreEqual(AdType.Rewarded.ToString(), shownEvents[0].GetParameter<string>("ad_type"));

            var rewardEvents = _analytics.GetEvents(AnalyticsEvents.AdReward);
            Assert.AreEqual(1, rewardEvents.Count);
            Assert.AreEqual(AdPlacement.DoubleReward.ToString(), rewardEvents[0].GetParameter<string>("placement"));

            Assert.AreEqual(1, _adService.ShowAdCallCount);
            Assert.IsTrue(_adService.HasShown(AdPlacement.DoubleReward));
        }

        [Test]
        public void CT_02_ShowAd_Failed_InvokesCallbackAndTracksFailedEvent_DoesNotGrantReward()
        {
            _adService.SetPlacementResult(AdPlacement.DoubleReward, AdShowResult.Failed);

            var callbackInvoked = false;
            var receivedResult = AdShowResult.Completed;

            _adService.ShowAd(AdPlacement.DoubleReward, result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(AdShowResult.Failed, receivedResult);

            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdShown));
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));
            Assert.IsFalse(_analytics.HasEvent(AnalyticsEvents.AdReward));

            var failedEvents = _analytics.GetEvents(AnalyticsEvents.AdFailed);
            Assert.AreEqual(1, failedEvents.Count);
            Assert.AreEqual(AdPlacement.DoubleReward.ToString(), failedEvents[0].GetParameter<string>("placement"));
            Assert.AreEqual(AdShowResult.Failed.ToString(), failedEvents[0].GetParameter<string>("reason"));
        }

        [Test]
        public void CT_Adversary_ShowAd_Skipped_TracksShownAndFailed_DoesNotGrantReward()
        {
            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Skipped);

            var callbackInvoked = false;
            var receivedResult = AdShowResult.Completed;

            _adService.ShowAd(AdPlacement.Revive, result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(AdShowResult.Skipped, receivedResult);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));
            Assert.IsFalse(_analytics.HasEvent(AnalyticsEvents.AdReward));
        }

        [Test]
        public void CT_Adversary_ShowAd_WhenPlacementNotAvailable_ReturnsNotAvailable_DoesNotGrantReward()
        {
            _adService.SetPlacementAvailable(AdPlacement.PreRunBoost, false);

            Assert.IsFalse(_adService.IsAdAvailable(AdPlacement.PreRunBoost));

            var callbackInvoked = false;
            var receivedResult = AdShowResult.Completed;

            _adService.ShowAd(AdPlacement.PreRunBoost, result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(AdShowResult.NotAvailable, receivedResult);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));
            Assert.IsFalse(_analytics.HasEvent(AnalyticsEvents.AdReward));
        }

        [Test]
        public void CT_Adversary_ShowAd_IndependentPlacementConfiguration_DoesNotCrossContaminate()
        {
            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Completed);
            _adService.SetPlacementResult(AdPlacement.DoubleReward, AdShowResult.Failed);

            var reviveResult = AdShowResult.NotAvailable;
            var doubleResult = AdShowResult.NotAvailable;

            _adService.ShowAd(AdPlacement.Revive, r => reviveResult = r);
            _adService.ShowAd(AdPlacement.DoubleReward, r => doubleResult = r);

            Assert.AreEqual(AdShowResult.Completed, reviveResult);
            Assert.AreEqual(AdShowResult.Failed, doubleResult);
            Assert.AreEqual(2, _adService.ShowAdCallCount);
        }

        [Test]
        public void CT_Adversary_ShowAd_NullAnalyticsService_CompletesSafelyWithoutThrowing()
        {
            var standaloneService = new MockAdService(null);
            var callbackInvoked = false;

            Assert.DoesNotThrow(() => standaloneService.ShowAd(AdPlacement.GeneralPowerCharge, _ => callbackInvoked = true));
            Assert.IsTrue(callbackInvoked);
            Assert.AreEqual(1, standaloneService.ShowAdCallCount);
        }

        [Test]
        public void CT_Adversary_ShowAd_InterLevel_TracksInterstitialType_WhileRewardedPlacementsTrackRewarded()
        {
            _adService.ShowAd(AdPlacement.InterLevel, _ => {});
            _adService.ShowAd(AdPlacement.CanteenDouble, _ => {});

            var shownEvents = _analytics.GetEvents(AnalyticsEvents.AdShown);
            Assert.AreEqual(2, shownEvents.Count);

            Assert.AreEqual(AdPlacement.InterLevel.ToString(), shownEvents[0].GetParameter<string>("placement"));
            Assert.AreEqual(AdType.Interstitial.ToString(), shownEvents[0].GetParameter<string>("ad_type"));

            Assert.AreEqual(AdPlacement.CanteenDouble.ToString(), shownEvents[1].GetParameter<string>("placement"));
            Assert.AreEqual(AdType.Rewarded.ToString(), shownEvents[1].GetParameter<string>("ad_type"));
        }

        [Test]
        public void CT_Adversary_ShowAd_ConsecutiveCalls_MaintainsAccurateHistoryAndCounts()
        {
            _adService.ShowAd(AdPlacement.DoubleReward, _ => {});
            _adService.ShowAd(AdPlacement.Revive, _ => {});
            _adService.ShowAd(AdPlacement.BarracksSpeedup, _ => {});

            Assert.AreEqual(3, _adService.ShowAdCallCount);
            Assert.AreEqual(3, _adService.History.Count);
            Assert.AreEqual(AdPlacement.DoubleReward, _adService.History[0].Placement);
            Assert.AreEqual(AdPlacement.Revive, _adService.History[1].Placement);
            Assert.AreEqual(AdPlacement.BarracksSpeedup, _adService.History[2].Placement);
            Assert.IsTrue(_adService.HasShown(AdPlacement.DoubleReward));
            Assert.IsTrue(_adService.HasShown(AdPlacement.Revive));
            Assert.IsTrue(_adService.HasShown(AdPlacement.BarracksSpeedup));
            Assert.IsFalse(_adService.HasShown(AdPlacement.InterLevel));
        }

        [Test]
        public void CT_Adversary_Reset_ClearsHistoryAndCustomConfiguration()
        {
            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Failed);
            _adService.SetPlacementAvailable(AdPlacement.DoubleReward, false);
            _adService.ShowAd(AdPlacement.Revive, _ => {});

            Assert.AreEqual(1, _adService.ShowAdCallCount);

            _adService.Reset();

            Assert.AreEqual(0, _adService.ShowAdCallCount);
            Assert.AreEqual(0, _adService.History.Count);
            Assert.IsTrue(_adService.IsAdAvailable(AdPlacement.DoubleReward));

            var resultAfterReset = AdShowResult.NotAvailable;
            _adService.ShowAd(AdPlacement.Revive, r => resultAfterReset = r);
            Assert.AreEqual(AdShowResult.Completed, resultAfterReset);
        }
    }
}
