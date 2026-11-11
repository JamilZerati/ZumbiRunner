using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Meta.Ads;
using Game.Presentation.UI;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.Gameplay
{
    public class GameplayReviveAdTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private EventBus _eventBus;
        private MockAnalyticsService _analytics;
        private MockAdService _adService;
        private FakeTimeProvider _timeProvider;
        private AdPolicy _adPolicy;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _analytics = new MockAnalyticsService();
            _adService = new MockAdService(_analytics);
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
            _adPolicy = new AdPolicy(_timeProvider, entitlements: null);
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

        private (LevelDirector director, RevivePromptWidget widget) CreateDirectorAndWidget()
        {
            var director = CreateComponent<LevelDirector>("LevelDirector");
            var config = new RunConfig { LevelId = "stage_01", InitialSquad = 10 };
            director.Initialize(config, null, _eventBus);

            var widget = CreateComponent<RevivePromptWidget>("RevivePromptWidget");
            var watchBtn = CreateComponent<Button>("WatchAdButton");
            var declineBtn = CreateComponent<Button>("DeclineButton");
            var promptText = CreateComponent<TextMeshProUGUI>("PromptText");
            var timerText = CreateComponent<TextMeshProUGUI>("TimerText");

            widget.BindComponents(watchBtn, declineBtn, promptText, timerText);
            widget.Initialize(director, _adService, _adPolicy, _analytics);

            return (director, widget);
        }

        [Test]
        public void CT_15_LevelDirector_WhenSquadZeros_OffersRevive_AndOnAdCompleted_RevivesSquadTo10WithoutEndingRun()
        {
            var (director, widget) = CreateDirectorAndWidget();
            widget.Hide();

            RunEndedEvent? runEnded = null;
            _eventBus.Subscribe<RunEndedEvent>(e => runEnded = e);

            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Completed);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 0));

            Assert.IsTrue(widget.gameObject.activeSelf);
            Assert.IsTrue(director.IsAwaitingRevive);
            Assert.IsTrue(director.IsRunActive);
            Assert.IsNull(runEnded);

            widget.HandleWatchAdClicked();

            Assert.IsFalse(widget.gameObject.activeSelf);
            Assert.IsTrue(director.ReviveUsedInCurrentRun);
            Assert.AreEqual(10, director.CurrentSquad);
            Assert.IsTrue(director.IsRunActive);
            Assert.IsTrue(director.IsReviveInvulnerable);
            Assert.AreEqual(1.5f, director.ReviveInvulnerabilityDurationRemaining);
            Assert.IsNull(runEnded);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdReward));
        }

        [Test]
        public void CT_16_LevelDirector_ReviveCanOnlyBeUsedOncePerRun_SecondDeathEndsRunImmediately()
        {
            var (director, widget) = CreateDirectorAndWidget();
            widget.Hide();

            RunEndedEvent? runEnded = null;
            _eventBus.Subscribe<RunEndedEvent>(e => runEnded = e);

            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Completed);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 0));
            widget.HandleWatchAdClicked();

            Assert.IsTrue(director.ReviveUsedInCurrentRun);
            Assert.IsTrue(director.IsRunActive);
            Assert.IsNull(runEnded);

            director.TickInvulnerability(1.5f);
            Assert.IsFalse(director.IsReviveInvulnerable);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 0));

            Assert.IsFalse(widget.gameObject.activeSelf);
            Assert.IsFalse(director.IsRunActive);
            Assert.IsNotNull(runEnded);
            Assert.IsFalse(runEnded.Value.Result.Victory);
        }

        [Test]
        public void RevivePromptWidget_WhenPlayerDeclines_ConfirmsDefeatViaEndRun()
        {
            var (director, widget) = CreateDirectorAndWidget();
            widget.Hide();

            RunEndedEvent? runEnded = null;
            _eventBus.Subscribe<RunEndedEvent>(e => runEnded = e);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 0));

            Assert.IsTrue(widget.gameObject.activeSelf);
            Assert.IsTrue(director.IsRunActive);

            widget.HandleDeclineClicked();

            Assert.IsFalse(widget.gameObject.activeSelf);
            Assert.IsFalse(director.IsRunActive);
            Assert.IsNotNull(runEnded);
            Assert.IsFalse(runEnded.Value.Result.Victory);
        }

        [Test]
        public void RevivePromptWidget_WhenAdFailsOrSkipped_ConfirmsDefeatAndTracksAdFailed()
        {
            var (director, widget) = CreateDirectorAndWidget();
            widget.Hide();

            RunEndedEvent? runEnded = null;
            _eventBus.Subscribe<RunEndedEvent>(e => runEnded = e);

            _adService.SetPlacementResult(AdPlacement.Revive, AdShowResult.Failed);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 0));

            Assert.IsTrue(widget.gameObject.activeSelf);

            widget.HandleWatchAdClicked();

            Assert.IsFalse(widget.gameObject.activeSelf);
            Assert.IsFalse(director.IsRunActive);
            Assert.IsNotNull(runEnded);
            Assert.IsFalse(runEnded.Value.Result.Victory);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));
        }

        [Test]
        public void ReviveProtection_Invulnerability_AbsorbsSquadDamageFor1Point5Seconds()
        {
            var (director, _) = CreateDirectorAndWidget();

            bool revived = director.ReviveSquad(10);
            Assert.IsTrue(revived);
            Assert.AreEqual(10, director.CurrentSquad);
            Assert.IsTrue(director.IsReviveInvulnerable);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 5));
            Assert.AreEqual(10, director.CurrentSquad);

            director.TickInvulnerability(1.5f);
            Assert.IsFalse(director.IsReviveInvulnerable);

            _eventBus.Publish(new SquadSizeChangedEvent(10, 5));
            Assert.AreEqual(5, director.CurrentSquad);
        }

        [Test]
        public void GeneralAbility_GrantBonusCharge_GrantsOneCharge_AndEnforcesOnePerRun()
        {
            var controller = CreateComponent<GeneralAbilityController>("GeneralAbilityController");
            var definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            definition.SetData("grenade", "Grenade", "Test", 25, null, null);

            controller.Initialize(definition, _eventBus, runConfig: new RunConfig { BonusAbilityCharges = 0 });

            Assert.AreEqual(0, controller.CurrentCharges);
            Assert.IsFalse(controller.BonusChargeUsedInCurrentRun);
            Assert.IsTrue(controller.CanGrantBonusCharge);

            bool firstGrant = controller.GrantBonusCharge(1);
            Assert.IsTrue(firstGrant);
            Assert.AreEqual(1, controller.CurrentCharges);
            Assert.IsTrue(controller.BonusChargeUsedInCurrentRun);
            Assert.IsFalse(controller.CanGrantBonusCharge);

            bool secondGrant = controller.GrantBonusCharge(1);
            Assert.IsFalse(secondGrant);
            Assert.AreEqual(1, controller.CurrentCharges);
        }

        [Test]
        public void GeneralAbility_TryRequestBonusChargeWithAd_WhenAdCompleted_GrantsCharge()
        {
            var controller = CreateComponent<GeneralAbilityController>("GeneralAbilityController");
            var definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            definition.SetData("grenade", "Grenade", "Test", 25, null, null);

            controller.Initialize(definition, _eventBus, runConfig: new RunConfig { BonusAbilityCharges = 0 });

            _adService.SetPlacementResult(AdPlacement.GeneralPowerCharge, AdShowResult.Completed);

            bool requested = controller.TryRequestBonusChargeWithAd(_adService, _adPolicy.CanShowRewarded, result =>
            {
                Assert.IsTrue(result);
            });

            Assert.IsTrue(requested);
            Assert.AreEqual(1, controller.CurrentCharges);
            Assert.IsTrue(controller.BonusChargeUsedInCurrentRun);

            bool secondRequest = controller.TryRequestBonusChargeWithAd(_adService, _adPolicy.CanShowRewarded, result =>
            {
                Assert.IsFalse(result);
            });

            Assert.IsFalse(secondRequest);
        }
    }
}
