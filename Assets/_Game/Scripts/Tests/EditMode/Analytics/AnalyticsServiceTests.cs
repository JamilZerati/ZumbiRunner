using System.Collections.Generic;
using Game.Core.Analytics;
using NUnit.Framework;

namespace Game.Tests.EditMode.Analytics
{
    public class AnalyticsServiceTests
    {
        private MockAnalyticsService _analytics;

        [SetUp]
        public void SetUp()
        {
            _analytics = new MockAnalyticsService();
        }

        [Test]
        public void CT_7_TrackFtueStep_RecordsCorrectParametersAndEventName()
        {
            _analytics.TrackFtueStep("swipe_movement", 1, "started");

            Assert.AreEqual(1, _analytics.Events.Count);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.FtueStep));

            var tracked = _analytics.Events[0];
            Assert.AreEqual(AnalyticsEvents.FtueStep, tracked.EventName);
            Assert.AreEqual("swipe_movement", tracked.GetParameter<string>("step_name"));
            Assert.AreEqual(1, tracked.GetParameter<int>("stage_index"));
            Assert.AreEqual("started", tracked.GetParameter<string>("status"));
        }

        [Test]
        public void TrackFtueStep_SequentialSteps_PreservesOrder()
        {
            _analytics.TrackFtueStep("swipe_movement", 1, "started");
            _analytics.TrackFtueStep("swipe_movement", 1, "completed");
            _analytics.TrackFtueStep("gate_choice", 2, "started");

            var ftueEvents = _analytics.GetEvents(AnalyticsEvents.FtueStep);
            Assert.AreEqual(3, ftueEvents.Count);
            Assert.AreEqual("started", ftueEvents[0].GetParameter<string>("status"));
            Assert.AreEqual("completed", ftueEvents[1].GetParameter<string>("status"));
            Assert.AreEqual("gate_choice", ftueEvents[2].GetParameter<string>("step_name"));
        }

        [Test]
        public void TrackFtueStep_NullArguments_SubstitutesEmptyStringsSafely()
        {
            _analytics.TrackFtueStep(null, 1, null);

            var tracked = _analytics.Events[0];
            Assert.AreEqual(string.Empty, tracked.GetParameter<string>("step_name"));
            Assert.AreEqual(1, tracked.GetParameter<int>("stage_index"));
            Assert.AreEqual(string.Empty, tracked.GetParameter<string>("status"));
        }

        [Test]
        public void TrackEvent_WithParameters_CopiesDictionarySafely()
        {
            var parameters = new Dictionary<string, object>
            {
                ["stage_id"] = "stage_01",
                ["score"] = 1500,
                ["is_boss"] = false
            };

            _analytics.TrackEvent(AnalyticsEvents.RunStart, parameters);

            Assert.AreEqual(1, _analytics.Events.Count);
            var tracked = _analytics.Events[0];
            Assert.AreEqual(AnalyticsEvents.RunStart, tracked.EventName);
            Assert.IsTrue(tracked.HasParameter("stage_id"));
            Assert.AreEqual("stage_01", tracked.GetParameter<string>("stage_id"));
            Assert.AreEqual(1500, tracked.GetParameter<int>("score"));
            Assert.AreEqual(false, tracked.GetParameter<bool>("is_boss"));
        }

        [Test]
        public void TrackEvent_NullParameters_CreatesEmptyDictionary()
        {
            _analytics.TrackEvent(AnalyticsEvents.RunStart, null);

            Assert.AreEqual(1, _analytics.Events.Count);
            var tracked = _analytics.Events[0];
            Assert.IsNotNull(tracked.Parameters);
            Assert.AreEqual(0, tracked.Parameters.Count);
            Assert.IsFalse(tracked.HasParameter("non_existent"));
            Assert.IsNull(tracked.GetParameter("non_existent"));
        }

        [Test]
        public void TrackEvent_NullOrEmptyEventName_Ignored()
        {
            _analytics.TrackEvent(null);
            _analytics.TrackEvent(string.Empty);

            Assert.AreEqual(0, _analytics.Events.Count);
            Assert.IsFalse(_analytics.HasEvent(null));
        }

        [Test]
        public void MockAnalyticsService_Clear_RemovesAllEvents()
        {
            _analytics.TrackFtueStep("step_1", 1, "completed");
            Assert.AreEqual(1, _analytics.Events.Count);

            _analytics.Clear();
            Assert.AreEqual(0, _analytics.Events.Count);
            Assert.IsFalse(_analytics.HasEvent(AnalyticsEvents.FtueStep));
        }

        [Test]
        public void AnalyticsEvents_Constants_ConformToCanonicalNames()
        {
            Assert.AreEqual("ftue_step", AnalyticsEvents.FtueStep);
            Assert.AreEqual("run_start", AnalyticsEvents.RunStart);
            Assert.AreEqual("run_end", AnalyticsEvents.RunEnd);
            Assert.AreEqual("ad_offer", AnalyticsEvents.AdOffer);
            Assert.AreEqual("ad_shown", AnalyticsEvents.AdShown);
            Assert.AreEqual("ad_reward", AnalyticsEvents.AdReward);
            Assert.AreEqual("iap_purchase", AnalyticsEvents.IapPurchase);
            Assert.AreEqual("objective_complete", AnalyticsEvents.ObjectiveComplete);
            Assert.AreEqual("track_claim", AnalyticsEvents.TrackClaim);
            Assert.AreEqual("economy_source", AnalyticsEvents.EconomySource);
            Assert.AreEqual("economy_sink", AnalyticsEvents.EconomySink);
        }
    }
}
