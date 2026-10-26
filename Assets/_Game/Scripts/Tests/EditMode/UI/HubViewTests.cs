using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Save;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    public class HubViewTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private Wallet _wallet;
        private StageMapService _stageMapService;
        private MockAnalyticsService _analytics;
        private UIManager _uiManager;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Hub);
            _wallet = new Wallet(_eventBus);
            _stageMapService = new StageMapService();
            _analytics = new MockAnalyticsService();

            _uiManager = CreateComponent<UIManager>("UIManager");
            _uiManager.Initialize(_stateMachine, _analytics, _eventBus);
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

        private (HubView hubView, StageSelectionWidget stageWidget) CreateHubViewHierarchy()
        {
            var hubGo = new GameObject("HubView");
            _createdObjects.Add(hubGo);
            var hubView = hubGo.AddComponent<HubView>();

            var coinsText = CreateComponent<TextMeshProUGUI>("CoinsText");
            var playButton = CreateComponent<Button>("PlayButton");
            var optionsButton = CreateComponent<Button>("OptionsButton");
            var debutButton = CreateComponent<Button>("DebutButton");
            var barracksButton = CreateComponent<Button>("BarracksButton");
            var shopButton = CreateComponent<Button>("ShopButton");
            var passButton = CreateComponent<Button>("PassButton");
            var feedbackText = CreateComponent<TextMeshProUGUI>("FeedbackText");

            var widgetGo = new GameObject("StageSelectionWidget");
            widgetGo.transform.SetParent(hubGo.transform);
            var stageWidget = widgetGo.AddComponent<StageSelectionWidget>();

            var stageTitle = CreateComponent<TextMeshProUGUI>("StageTitle");
            var stageStars = CreateComponent<TextMeshProUGUI>("StageStars");
            var lockIndicator = new GameObject("LockIndicator");
            _createdObjects.Add(lockIndicator);
            var prevButton = CreateComponent<Button>("PrevButton");
            var nextButton = CreateComponent<Button>("NextButton");

            stageWidget.BindComponents(stageTitle, stageStars, lockIndicator, prevButton, nextButton);
            hubView.BindComponents(coinsText, playButton, optionsButton, debutButton, barracksButton, shopButton, passButton, stageWidget, feedbackText);

            hubView.Initialize(_wallet, _stageMapService, _stateMachine, _uiManager, _eventBus, _analytics);

            return (hubView, stageWidget);
        }

        [Test]
        public void HubView_LayerType_IsHub()
        {
            var (hubView, _) = CreateHubViewHierarchy();
            Assert.AreEqual(UILayerType.Hub, hubView.LayerType);
        }

        [Test]
        public void CoinsDisplay_ReactiveToWalletChanges()
        {
            _wallet.Add(CurrencyKind.Coins, 150, "initial_gift");

            var (hubView, _) = CreateHubViewHierarchy();
            Assert.AreEqual("150", hubView.CoinsText.text);

            _wallet.Add(CurrencyKind.Coins, 50, "stage_reward");
            Assert.AreEqual("200", hubView.CoinsText.text);

            _wallet.TrySpend(CurrencyKind.Coins, 80, "shop_upgrade");
            Assert.AreEqual("120", hubView.CoinsText.text);
        }

        [Test]
        public void StageSelection_SelectsHighestUnlockedByDefault()
        {
            _stageMapService.RecordStageResult("stage_01", stars: 2, isBoss: false);
            _stageMapService.RecordStageResult("stage_02", stars: 3, isBoss: false);

            var (hubView, stageWidget) = CreateHubViewHierarchy();

            Assert.AreEqual("stage_03", stageWidget.SelectedStageId);
            Assert.AreEqual("stage_03", hubView.SelectedStageId);
        }

        [Test]
        public void StageSelection_AllowsUnlockedStage_AndRejectsLockedStage()
        {
            var (hubView, stageWidget) = CreateHubViewHierarchy();
            Assert.AreEqual("stage_01", hubView.SelectedStageId);

            bool selectLocked = hubView.SelectStage("stage_02");
            Assert.IsFalse(selectLocked);
            Assert.AreEqual("stage_01", hubView.SelectedStageId);

            _stageMapService.RecordStageResult("stage_01", stars: 1, isBoss: false);
            Assert.IsTrue(_stageMapService.IsStageUnlocked("stage_02"));

            bool selectUnlocked = hubView.SelectStage("stage_02");
            Assert.IsTrue(selectUnlocked);
            Assert.AreEqual("stage_02", hubView.SelectedStageId);
            Assert.AreEqual("stage_02", stageWidget.StageTitleText.text);
        }

        [Test]
        public void StartSelectedRun_WhenStageUnlocked_TransitionsToRunState_AndEmitsRunStartedEvent()
        {
            var (hubView, _) = CreateHubViewHierarchy();
            hubView.Open();

            RunStartedEvent emittedEvent = default;
            bool eventFired = false;
            _eventBus.Subscribe<RunStartedEvent>(e =>
            {
                emittedEvent = e;
                eventFired = true;
            });

            bool started = hubView.StartSelectedRun();

            Assert.IsTrue(started);
            Assert.AreEqual(GameState.Run, _stateMachine.CurrentState);
            Assert.IsTrue(eventFired);
            Assert.AreEqual("stage_01", emittedEvent.LevelId);
            Assert.AreEqual("stage_01", emittedEvent.Config.LevelId);
        }

        private class LockedStageMapService : IStageMapService
        {
            public bool IsStageUnlocked(string stageId) => false;
            public int GetStars(string stageId) => 0;
            public void RecordStageResult(string stageId, int stars, bool isBoss) { }
            public string GetHighestUnlockedStage() => null;
            public IReadOnlyList<string> GetAllBossStages() => System.Array.Empty<string>();
        }

        [Test]
        public void StartSelectedRun_WhenStageLocked_BlocksTransition()
        {
            var (hubView, _) = CreateHubViewHierarchy();
            hubView.Initialize(_wallet, new LockedStageMapService(), _stateMachine, _uiManager, _eventBus, _analytics);
            hubView.Open();

            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
            bool started = hubView.StartSelectedRun();

            Assert.IsFalse(started);
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
        }

        [Test]
        public void OptionsButton_OpensOptionsViewViaUIManager()
        {
            var (hubView, _) = CreateHubViewHierarchy();
            _uiManager.RegisterWindow(hubView);

            var optionsView = CreateComponent<OptionsView>("OptionsView");
            _uiManager.RegisterWindow(optionsView);

            hubView.Open();
            Assert.IsTrue(hubView.IsOpen);
            Assert.IsFalse(optionsView.IsOpen);

            bool optionsRequested = false;
            hubView.OnOptionsRequested += () => optionsRequested = true;

            hubView.OptionsButton.onClick.Invoke();

            Assert.IsTrue(optionsRequested);
            Assert.IsTrue(_uiManager.IsWindowOpen<OptionsView>());
            Assert.IsTrue(optionsView.IsOpen);
        }

        [Test]
        public void NavigationShortcuts_TriggerFeedbackAndEvents()
        {
            var (hubView, _) = CreateHubViewHierarchy();

            string lastClicked = null;
            hubView.OnNavigationShortcutClicked += s => lastClicked = s;

            hubView.BarracksButton.onClick.Invoke();
            Assert.AreEqual("Quartel", hubView.LastNavigationShortcut);
            Assert.AreEqual("Quartel", hubView.NavigationFeedbackText.text);
            Assert.AreEqual("Quartel", lastClicked);

            hubView.ShopButton.onClick.Invoke();
            Assert.AreEqual("Loja", hubView.LastNavigationShortcut);
            Assert.AreEqual("Loja", hubView.NavigationFeedbackText.text);
            Assert.AreEqual("Loja", lastClicked);

            hubView.PassButton.onClick.Invoke();
            Assert.AreEqual("Passe", hubView.LastNavigationShortcut);
            Assert.AreEqual("Passe", hubView.NavigationFeedbackText.text);
            Assert.AreEqual("Passe", lastClicked);
        }

        [Test]
        public void DebutJourneyButton_InvokesHookAndEvent()
        {
            var (hubView, _) = CreateHubViewHierarchy();

            bool eventFired = false;
            bool hookFired = false;

            hubView.OnDebutJourneyRequested += () => eventFired = true;
            hubView.DebutJourneyHook = () => hookFired = true;

            hubView.DebutJourneyButton.onClick.Invoke();

            Assert.IsTrue(eventFired);
            Assert.IsTrue(hookFired);
        }

        [Test]
        public void StageSelectionWidget_PrevNextNavigation_SelectsStages()
        {
            _stageMapService.RecordStageResult("stage_01", stars: 1, isBoss: false);
            _stageMapService.RecordStageResult("stage_02", stars: 2, isBoss: false);

            var (hubView, stageWidget) = CreateHubViewHierarchy();
            stageWidget.SelectStage("stage_01");
            Assert.AreEqual("stage_01", stageWidget.SelectedStageId);

            stageWidget.NextButton.onClick.Invoke();
            Assert.AreEqual("stage_02", stageWidget.SelectedStageId);

            stageWidget.PrevButton.onClick.Invoke();
            Assert.AreEqual("stage_01", stageWidget.SelectedStageId);
        }
    }
}
