using System.Collections.Generic;
using Game.Core.Analytics;
using Game.Gameplay.Tutorial;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.Tutorial
{
    public class TutorialGuideControllerTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private MockAnalyticsService _analytics;
        private FtueStepProgress _progress;
        private TutorialGuideController _controller;

        [SetUp]
        public void SetUp()
        {
            _analytics = new MockAnalyticsService();
            _progress = new FtueStepProgress();
            _controller = new TutorialGuideController(_progress, _analytics);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();
        }

        private T CreateComponent<T>(string name = null) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            _createdObjects.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void CheckTutorialStep_Stages1To3_TriggersExpectedStepsAndTelemetry()
        {
            _controller.CheckTutorialStep(1);

            Assert.IsTrue(_controller.HasActiveStep);
            Assert.AreEqual(FtueStepProgress.SwipeStep, _controller.ActiveStepId);
            Assert.AreEqual(1, _controller.ActiveStageIndex);
            Assert.AreEqual(1, _analytics.Events.Count);
            Assert.AreEqual(AnalyticsEvents.FtueStep, _analytics.Events[0].EventName);
            Assert.AreEqual("swipe_movement", _analytics.Events[0].Parameters["step_name"]);
            Assert.AreEqual(1, _analytics.Events[0].Parameters["stage_index"]);
            Assert.AreEqual("started", _analytics.Events[0].Parameters["status"]);

            _controller.CompleteTutorialStep(FtueStepProgress.SwipeStep, 1);
            Assert.IsFalse(_controller.HasActiveStep);
            Assert.AreEqual(2, _analytics.Events.Count);
            Assert.AreEqual("completed", _analytics.Events[1].Parameters["status"]);

            _controller.CheckTutorialStep(2);
            Assert.IsTrue(_controller.HasActiveStep);
            Assert.AreEqual(FtueStepProgress.GateStep, _controller.ActiveStepId);
            Assert.AreEqual(2, _controller.ActiveStageIndex);
            Assert.AreEqual(3, _analytics.Events.Count);
            Assert.AreEqual("gate_choice", _analytics.Events[2].Parameters["step_name"]);
            Assert.AreEqual("started", _analytics.Events[2].Parameters["status"]);

            _controller.CompleteTutorialStep(FtueStepProgress.GateStep, 2);
            Assert.IsFalse(_controller.HasActiveStep);

            _controller.CheckTutorialStep(3);
            Assert.IsTrue(_controller.HasActiveStep);
            Assert.AreEqual(FtueStepProgress.CageStep, _controller.ActiveStepId);
            Assert.AreEqual(3, _controller.ActiveStageIndex);
            Assert.AreEqual(5, _analytics.Events.Count);
            Assert.AreEqual("cage_rescue", _analytics.Events[4].Parameters["step_name"]);
            Assert.AreEqual("started", _analytics.Events[4].Parameters["status"]);
        }

        [Test]
        public void CheckTutorialStep_StageBeyond3_OrInvalidStage_DoesNotTriggerStep()
        {
            _controller.CheckTutorialStep(0);
            Assert.IsFalse(_controller.HasActiveStep);
            Assert.AreEqual(0, _analytics.Events.Count);

            _controller.CheckTutorialStep(4);
            Assert.IsFalse(_controller.HasActiveStep);
            Assert.AreEqual(0, _analytics.Events.Count);

            _controller.CheckTutorialStep(-1);
            Assert.IsFalse(_controller.HasActiveStep);
            Assert.AreEqual(0, _analytics.Events.Count);
        }

        [Test]
        public void CheckTutorialStep_AlreadyCompletedStep_DoesNotTriggerAgain()
        {
            _progress.CompleteStep(FtueStepProgress.SwipeStep);

            _controller.CheckTutorialStep(1);

            Assert.IsFalse(_controller.HasActiveStep);
            Assert.AreEqual(0, _analytics.Events.Count);
        }

        [Test]
        public void CheckTutorialStep_AlreadyActiveStep_DoesNotEmitDuplicateStartedTelemetry()
        {
            _controller.CheckTutorialStep(1);
            Assert.AreEqual(1, _analytics.Events.Count);

            _controller.CheckTutorialStep(1);
            Assert.AreEqual(1, _analytics.Events.Count);
        }

        [Test]
        public void CompleteTutorialStep_AlreadyCompleted_DoesNotEmitDuplicateCompletedTelemetry()
        {
            _controller.CheckTutorialStep(1);
            Assert.AreEqual(1, _analytics.Events.Count);

            _controller.CompleteTutorialStep(FtueStepProgress.SwipeStep, 1);
            Assert.AreEqual(2, _analytics.Events.Count);

            _controller.CompleteTutorialStep(FtueStepProgress.SwipeStep, 1);
            Assert.AreEqual(2, _analytics.Events.Count);
        }

        [Test]
        public void TutorialGuideView_ShowAndHideStep_ControlsWindowAndUIState()
        {
            var view = CreateComponent<TutorialGuideView>("TutorialGuideView");
            var bannerRoot = new GameObject("BannerRoot");
            _createdObjects.Add(bannerRoot);
            bannerRoot.transform.SetParent(view.transform);
            var text = CreateComponent<TextMeshProUGUI>("InstructionText");
            var icon = CreateComponent<Image>("StepIcon");

            view.BindComponents(text, bannerRoot, icon);

            view.ShowStep("swipe_movement", "Arraste para mover");

            Assert.IsTrue(view.IsOpen);
            Assert.AreEqual("swipe_movement", view.CurrentStepId);
            Assert.AreEqual("Arraste para mover", view.CurrentMessage);
            Assert.AreEqual("Arraste para mover", text.text);
            Assert.IsTrue(bannerRoot.activeSelf);
            Assert.AreEqual(UILayerType.TutorialOverlay, view.LayerType);

            view.HideStep();

            Assert.IsFalse(view.IsOpen);
            Assert.IsNull(view.CurrentStepId);
            Assert.IsNull(view.CurrentMessage);
            Assert.IsFalse(bannerRoot.activeSelf);
        }

        [Test]
        public void TutorialGuideView_Initialize_BindsToControllerLifecycle()
        {
            var view = CreateComponent<TutorialGuideView>("TutorialGuideView");
            var bannerRoot = new GameObject("BannerRoot");
            _createdObjects.Add(bannerRoot);
            bannerRoot.transform.SetParent(view.transform);
            var text = CreateComponent<TextMeshProUGUI>("InstructionText");

            view.BindComponents(text, bannerRoot);
            view.Initialize(_controller);

            Assert.IsFalse(view.IsOpen);

            _controller.CheckTutorialStep(1);

            Assert.IsTrue(view.IsOpen);
            Assert.AreEqual(FtueStepProgress.SwipeStep, view.CurrentStepId);
            StringAssert.Contains("Deslize", text.text);

            _controller.CompleteTutorialStep(FtueStepProgress.SwipeStep, 1);

            Assert.IsFalse(view.IsOpen);
            Assert.IsNull(view.CurrentStepId);
        }
    }
}
