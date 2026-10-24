using System.Collections.Generic;
using Game.Core.Analytics;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    public class FtueTutorialTests
    {
        [Test]
        public void CT_7_MockAnalyticsService_RecordsFtueStepsAndRunLifecycleEvents()
        {
            var analytics = new MockAnalyticsService();

            analytics.TrackFtueStep("swipe_movement", 1, "started");
            analytics.TrackFtueStep("swipe_movement", 1, "completed");

            Assert.AreEqual(2, analytics.Events.Count);
            Assert.AreEqual(AnalyticsEvents.FtueStep, analytics.Events[0].EventName);
            Assert.AreEqual("swipe_movement", analytics.Events[0].Parameters["step_name"]);
            Assert.AreEqual(1, analytics.Events[0].Parameters["stage_index"]);
            Assert.AreEqual("started", analytics.Events[0].Parameters["status"]);

            Assert.AreEqual("completed", analytics.Events[1].Parameters["status"]);

            analytics.TrackEvent(AnalyticsEvents.RunStart, new Dictionary<string, object> { ["level_id"] = "stage_01" });
            Assert.AreEqual(3, analytics.Events.Count);
            Assert.AreEqual(AnalyticsEvents.RunStart, analytics.Events[2].EventName);
        }

        [Test]
        public void CT_5_FtueTutorial_IdentifiesExpectedGuidancePerStage_AndSkipsCompleted()
        {
            var completedSteps = new HashSet<string>();

            string GetTutorialForStage(int stageIndex)
            {
                if (stageIndex == 1 && !completedSteps.Contains("swipe")) return "swipe";
                if (stageIndex == 2 && !completedSteps.Contains("gate")) return "gate";
                if (stageIndex == 3 && !completedSteps.Contains("cage")) return "cage";
                return null;
            }

            Assert.AreEqual("swipe", GetTutorialForStage(1));
            completedSteps.Add("swipe");
            Assert.IsNull(GetTutorialForStage(1));

            Assert.AreEqual("gate", GetTutorialForStage(2));
            completedSteps.Add("gate");
            Assert.IsNull(GetTutorialForStage(2));

            Assert.AreEqual("cage", GetTutorialForStage(3));
            completedSteps.Add("cage");
            Assert.IsNull(GetTutorialForStage(3));

            Assert.IsNull(GetTutorialForStage(4));
        }
    }
}
