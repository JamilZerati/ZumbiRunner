using System.Collections.Generic;
using Game.Core.Analytics;
using Game.Gameplay.Tutorial;
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
            var progress = new FtueStepProgress();

            Assert.AreEqual(FtueStepProgress.SwipeStep, progress.GetTutorialStepForStage(1));
            Assert.IsFalse(progress.IsStepCompleted(FtueStepProgress.SwipeStep));

            Assert.IsTrue(progress.CompleteStep(FtueStepProgress.SwipeStep));
            Assert.IsTrue(progress.IsStepCompleted(FtueStepProgress.SwipeStep));
            Assert.IsNull(progress.GetTutorialStepForStage(1));

            Assert.AreEqual(FtueStepProgress.GateStep, progress.GetTutorialStepForStage(2));
            Assert.IsTrue(progress.CompleteStep(FtueStepProgress.GateStep));
            Assert.IsNull(progress.GetTutorialStepForStage(2));

            Assert.AreEqual(FtueStepProgress.CageStep, progress.GetTutorialStepForStage(3));
            Assert.IsTrue(progress.CompleteStep(FtueStepProgress.CageStep));
            Assert.IsNull(progress.GetTutorialStepForStage(3));

            Assert.IsNull(progress.GetTutorialStepForStage(4));
            Assert.IsNull(progress.GetTutorialStepForStage(0));

            var restored = new FtueStepProgress(progress.CompletedSteps);
            Assert.IsTrue(restored.IsStepCompleted(FtueStepProgress.SwipeStep));
            Assert.IsTrue(restored.IsStepCompleted(FtueStepProgress.GateStep));
            Assert.IsTrue(restored.IsStepCompleted(FtueStepProgress.CageStep));
            Assert.IsNull(restored.GetTutorialStepForStage(1));
        }
    }
}
