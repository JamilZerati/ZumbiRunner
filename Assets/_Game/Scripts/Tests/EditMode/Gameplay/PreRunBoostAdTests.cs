using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Ads;
using Game.Meta.Run;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.Gameplay
{
    public class PreRunBoostAdTests
    {
        private class FakeStageMapService : IStageMapService
        {
            private readonly HashSet<string> _unlocked = new() { "stage_01", "stage_02" };
            public string GetHighestUnlockedStage() => "stage_01";
            public bool IsStageUnlocked(string stageId) => _unlocked.Contains(stageId);
            public int GetStars(string stageId) => 3;
            public void RecordStageResult(string stageId, int stars, bool isBoss) => _unlocked.Add(stageId);
            public IReadOnlyList<string> GetAllBossStages() => Array.Empty<string>();
        }

        private readonly List<GameObject> _createdObjects = new();
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private Wallet _wallet;
        private MockAnalyticsService _analytics;
        private MockAdService _adService;
        private FakeTimeProvider _timeProvider;
        private AdPolicy _adPolicy;
        private RunConfigBuilder _configBuilder;
        private FakeStageMapService _stageMapService;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Boot);
            _stateMachine.TryTransition(GameState.Hub);
            _wallet = new Wallet(_eventBus);
            _analytics = new MockAnalyticsService();
            _adService = new MockAdService(_analytics);
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
            _adPolicy = new AdPolicy(_timeProvider, entitlements: null);
            _configBuilder = new RunConfigBuilder();
            _stageMapService = new FakeStageMapService();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
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

        private (HubView hubView, StageSelectionWidget stageWidget) CreateHubView()
        {
            var hubView = CreateComponent<HubView>("HubView");
            var coinsText = CreateComponent<TextMeshProUGUI>("CoinsText");
            var playBtn = CreateComponent<Button>("PlayButton");
            var boostBtn = CreateComponent<Button>("BoostSquadButton");
            var boostStatus = CreateComponent<TextMeshProUGUI>("BoostSquadStatus");

            var stageWidget = CreateComponent<StageSelectionWidget>("StageSelectionWidget");
            var stageTitle = CreateComponent<TextMeshProUGUI>("StageTitle");
            var stageStars = CreateComponent<TextMeshProUGUI>("StageStars");
            var stageBoostBtn = CreateComponent<Button>("StageBoostBtn");

            stageWidget.BindComponents(title: stageTitle, stars: stageStars, boostSquad: stageBoostBtn);

            hubView.BindComponents(
                coins: coinsText,
                play: playBtn,
                stageWidget: stageWidget,
                boostSquad: boostBtn,
                boostSquadText: boostStatus);

            hubView.Initialize(
                _wallet,
                _stageMapService,
                _stateMachine,
                uiManager: null,
                _eventBus,
                _analytics,
                mutationCatalog: null,
                progressionService: null,
                _adService,
                _adPolicy,
                _configBuilder);

            return (hubView, stageWidget);
        }

        [Test]
        public void CT_17_HubView_PreRunBoost_WithAdCompleted_GrantsPlus10InitialSquadInRunConfig()
        {
            var (hubView, _) = CreateHubView();
            hubView.Open();

            RunStartedEvent? started = null;
            _eventBus.Subscribe<RunStartedEvent>(e => started = e);

            Assert.IsFalse(hubView.IsPreRunBoostActive);
            Assert.IsTrue(hubView.CanRequestPreRunBoost);

            _adService.SetPlacementResult(AdPlacement.PreRunBoost, AdShowResult.Completed);
            hubView.RequestPreRunBoostWithAd();

            Assert.IsTrue(hubView.IsPreRunBoostActive);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdReward));

            hubView.SelectStage("stage_01");
            bool startedOk = hubView.StartSelectedRun();

            Assert.IsTrue(startedOk);
            Assert.IsNotNull(started);
            Assert.AreEqual("stage_01", started.Value.LevelId);
            Assert.AreEqual(20, started.Value.Config.InitialSquad);
        }

        [Test]
        public void HubView_PreRunBoost_ConsumedAfterRunStarts()
        {
            var (hubView, _) = CreateHubView();
            hubView.Open();

            RunStartedEvent? firstRun = null;
            _eventBus.Subscribe<RunStartedEvent>(e => firstRun = e);

            _adService.SetPlacementResult(AdPlacement.PreRunBoost, AdShowResult.Completed);
            hubView.RequestPreRunBoostWithAd();
            Assert.IsTrue(hubView.IsPreRunBoostActive);

            hubView.SelectStage("stage_01");
            hubView.StartSelectedRun();

            Assert.IsNotNull(firstRun);
            Assert.AreEqual(20, firstRun.Value.Config.InitialSquad);
            Assert.IsFalse(hubView.IsPreRunBoostActive);

            _stateMachine.TryTransition(GameState.Hub);
            RunStartedEvent? secondRun = null;
            _eventBus.Subscribe<RunStartedEvent>(e => secondRun = e);

            hubView.StartSelectedRun();
            Assert.IsNotNull(secondRun);
            Assert.AreEqual(10, secondRun.Value.Config.InitialSquad);
        }

        [Test]
        public void HubView_PreRunBoost_WhenAdSkippedOrFailed_DoesNotActivateBoost()
        {
            var (hubView, _) = CreateHubView();
            hubView.Open();

            RunStartedEvent? started = null;
            _eventBus.Subscribe<RunStartedEvent>(e => started = e);

            _adService.SetPlacementResult(AdPlacement.PreRunBoost, AdShowResult.Failed);
            hubView.RequestPreRunBoostWithAd();

            Assert.IsFalse(hubView.IsPreRunBoostActive);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));

            hubView.SelectStage("stage_01");
            hubView.StartSelectedRun();

            Assert.IsNotNull(started);
            Assert.AreEqual(10, started.Value.Config.InitialSquad);
        }

        [Test]
        public void HubView_PreRunBoost_WhenPolicyDisallows_DoesNotShowAd()
        {
            var (hubView, _) = CreateHubView();
            hubView.Open();

            _adPolicy.State.DailyRewardedCount = _adPolicy.Definition.DailyRewardedLimit;

            Assert.IsFalse(hubView.CanRequestPreRunBoost);
            hubView.RequestPreRunBoostWithAd();

            Assert.AreEqual(0, _adService.ShowAdCallCount);
            Assert.IsFalse(hubView.IsPreRunBoostActive);
        }

        [Test]
        public void StageSelectionWidget_BoostButton_TriggersHubViewPreRunBoostRequest()
        {
            var (hubView, stageWidget) = CreateHubView();
            hubView.Open();

            _adService.SetPlacementResult(AdPlacement.PreRunBoost, AdShowResult.Completed);

            stageWidget.BoostSquadButton.onClick.Invoke();

            Assert.IsTrue(hubView.IsPreRunBoostActive);
            Assert.AreEqual(1, _adService.ShowAdCallCount);
        }
    }
}
