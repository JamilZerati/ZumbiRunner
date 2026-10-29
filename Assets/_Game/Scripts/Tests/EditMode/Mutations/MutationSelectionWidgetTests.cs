using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Data.Mutations;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class MutationSelectionWidgetTests
    {
        private readonly List<Object> _createdObjects = new();

        private class FakeProgressionService : IProgressionService
        {
            public bool AreMutationsUnlocked { get; set; }
            public bool ProcessRunResult(RunResult result, IRewardSink sink, out IReward grantedReward)
            {
                grantedReward = null;
                return true;
            }
            public bool IsContentUnlocked(string contentId) => true;
            public bool IsBossDefeated(string levelId) => AreMutationsUnlocked;
        }

        private class FakeMutationCatalog : IMutationCatalog
        {
            private readonly Dictionary<string, MutationDefinition> _mutations = new(StringComparer.OrdinalIgnoreCase);

            public IReadOnlyList<MutationDefinition> Entries => new List<MutationDefinition>(_mutations.Values);
            public IReadOnlyList<MutationDefinition> Mutations => Entries;

            public void Add(MutationDefinition def) => _mutations[def.Id] = def;
            public bool TryGet(string id, out MutationDefinition definition)
            {
                definition = null;
                return !string.IsNullOrEmpty(id) && _mutations.TryGetValue(id, out definition);
            }
            public bool TryGetMutation(string id, out MutationDefinition mutation) => TryGet(id, out mutation);
            public MutationDefinition GetMutation(string id) => _mutations.TryGetValue(id, out var def) ? def : null;

            public IReadOnlyDictionary<string, float> GetRewardBonuses()
            {
                var dict = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in _mutations)
                {
                    dict[pair.Key] = pair.Value.RewardBonus;
                }
                return dict;
            }
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

        private MutationDefinition CreateDefinition(string id, string name, float bonus)
        {
            var def = ScriptableObject.CreateInstance<MutationDefinition>();
            _createdObjects.Add(def);
            def.SetData(id, name, $"{name} description", bonus, null);
            return def;
        }

        private FakeMutationCatalog CreateDefaultFakeCatalog()
        {
            var catalog = new FakeMutationCatalog();
            catalog.Add(CreateDefinition("armored", "Blindados", 0.5f));
            catalog.Add(CreateDefinition("frenzy", "Frenesi", 0.5f));
            catalog.Add(CreateDefinition("fog", "Neblina", 0.25f));
            catalog.Add(CreateDefinition("last_soldier", "Último Soldado", 1.0f));
            catalog.Add(CreateDefinition("no_multiplication", "Sem Multiplicação", 0.5f));
            return catalog;
        }

        private MutationToggleItem CreateToggleItem(string name)
        {
            var item = CreateComponent<MutationToggleItem>(name);
            var iconImg = CreateComponent<Image>($"{name}_Icon");
            var nameTxt = CreateComponent<TextMeshProUGUI>($"{name}_Name");
            var bonusTxt = CreateComponent<TextMeshProUGUI>($"{name}_Bonus");
            var btn = CreateComponent<Button>($"{name}_Btn");
            var indicator = new GameObject($"{name}_Indicator");
            _createdObjects.Add(indicator);

            item.BindComponents(iconImg, nameTxt, bonusTxt, btn, indicator);
            return item;
        }

        private MutationSelectionWidget CreateWidget(FakeMutationCatalog catalog, out List<MutationToggleItem> items)
        {
            var widget = CreateComponent<MutationSelectionWidget>("MutationSelectionWidget");
            var container = new GameObject("ItemsContainer");
            _createdObjects.Add(container);

            var bonusText = CreateComponent<TextMeshProUGUI>("TotalBonusText");
            var lockedOverlay = new GameObject("LockedOverlay");
            _createdObjects.Add(lockedOverlay);
            var lockedReason = CreateComponent<TextMeshProUGUI>("LockedReason");

            items = new List<MutationToggleItem>();
            foreach (var def in catalog.Entries)
            {
                var item = CreateToggleItem($"Item_{def.Id}");
                item.transform.SetParent(container.transform);
                items.Add(item);
            }

            widget.BindComponents(container.transform, null, bonusText, lockedOverlay, lockedReason, items);
            return widget;
        }

        [Test]
        public void CT_8_Widget_LockedState_WhenMutationsNotUnlocked_ShowsLockOverlay_AndBlocksSelection()
        {
            var catalog = CreateDefaultFakeCatalog();
            var widget = CreateWidget(catalog, out var items);
            var progression = new FakeProgressionService { AreMutationsUnlocked = false };

            widget.Initialize(catalog, progression);

            Assert.IsFalse(widget.AreMutationsUnlocked);
            Assert.IsTrue(widget.LockedOverlay.activeSelf);
            Assert.IsNotEmpty(widget.LockedReasonText.text);

            for (int i = 0; i < items.Count; i++)
            {
                Assert.IsFalse(items[i].ToggleButton.interactable);
            }

            bool toggled = widget.ToggleMutation("armored");
            Assert.IsFalse(toggled);
            Assert.IsEmpty(widget.SelectedMutationIds);
            Assert.AreEqual(0f, widget.TotalBonusPercentage);
            Assert.AreEqual("+0% Moedas", widget.TotalBonusText.text);
        }

        [Test]
        public void CT_8_Widget_UnlockedState_HidesOverlay_AndEnablesInteraction()
        {
            var catalog = CreateDefaultFakeCatalog();
            var widget = CreateWidget(catalog, out var items);
            var progression = new FakeProgressionService { AreMutationsUnlocked = true };

            widget.Initialize(catalog, progression);

            Assert.IsTrue(widget.AreMutationsUnlocked);
            Assert.IsFalse(widget.LockedOverlay.activeSelf);

            for (int i = 0; i < items.Count; i++)
            {
                Assert.IsTrue(items[i].ToggleButton.interactable);
            }

            bool toggled = widget.ToggleMutation("armored");
            Assert.IsTrue(toggled);
            Assert.AreEqual(1, widget.SelectedMutationIds.Count);
            Assert.AreEqual("armored", widget.SelectedMutationIds[0]);
        }

        [Test]
        public void CT_9_Widget_SelectionCap_FourthMutationSelectionRejected_KeepingMaximumOfThree()
        {
            var catalog = CreateDefaultFakeCatalog();
            var widget = CreateWidget(catalog, out _);
            var progression = new FakeProgressionService { AreMutationsUnlocked = true };

            widget.Initialize(catalog, progression);

            Assert.IsTrue(widget.ToggleMutation("armored"));
            Assert.IsTrue(widget.ToggleMutation("frenzy"));
            Assert.IsTrue(widget.ToggleMutation("fog"));
            Assert.AreEqual(3, widget.SelectedMutationIds.Count);

            bool fourthAttempt = widget.ToggleMutation("last_soldier");
            Assert.IsFalse(fourthAttempt);
            Assert.AreEqual(3, widget.SelectedMutationIds.Count);
            Assert.Contains("armored", (System.Collections.ICollection)widget.SelectedMutationIds);
            Assert.Contains("frenzy", (System.Collections.ICollection)widget.SelectedMutationIds);
            Assert.Contains("fog", (System.Collections.ICollection)widget.SelectedMutationIds);
            Assert.IsFalse(new List<string>(widget.SelectedMutationIds).Contains("last_soldier"));
        }

        [Test]
        public void Widget_ToggleDeselectsMutation_AndRecalculatesAccumulatedBonusAccurately()
        {
            var catalog = CreateDefaultFakeCatalog();
            var widget = CreateWidget(catalog, out _);
            var progression = new FakeProgressionService { AreMutationsUnlocked = true };

            widget.Initialize(catalog, progression);

            // armored = +50%
            widget.ToggleMutation("armored");
            Assert.AreEqual(0.5f, widget.TotalBonusPercentage, 1e-4f);
            Assert.AreEqual("+50% Moedas", widget.TotalBonusText.text);

            // frenzy = +50% -> total = 1.0f (+100%)
            widget.ToggleMutation("frenzy");
            Assert.AreEqual(1.0f, widget.TotalBonusPercentage, 1e-4f);
            Assert.AreEqual("+100% Moedas", widget.TotalBonusText.text);

            // Deselect armored -> total returns to 0.5f (+50%)
            widget.ToggleMutation("armored");
            Assert.AreEqual(0.5f, widget.TotalBonusPercentage, 1e-4f);
            Assert.AreEqual("+50% Moedas", widget.TotalBonusText.text);

            // Deselect frenzy -> total returns to 0
            widget.ToggleMutation("frenzy");
            Assert.AreEqual(0f, widget.TotalBonusPercentage, 1e-4f);
            Assert.AreEqual("+0% Moedas", widget.TotalBonusText.text);
        }

        [Test]
        public void MutationToggleItem_BindAndClick_UpdatesVisualsAndInvokesCallback()
        {
            var item = CreateToggleItem("TestItem");
            var def = CreateDefinition("fog", "Neblina Densa", 0.25f);

            string clickedId = null;
            item.Bind(def, isSelected: false, id => clickedId = id);

            Assert.AreEqual("fog", item.MutationId);
            Assert.AreEqual("Neblina Densa", item.NameText.text);
            Assert.AreEqual("+25%", item.BonusText.text);
            Assert.IsFalse(item.IsSelected);
            Assert.IsFalse(item.SelectedIndicator.activeSelf);

            item.ToggleButton.onClick.Invoke();
            Assert.AreEqual("fog", clickedId);

            item.SetSelected(true);
            Assert.IsTrue(item.IsSelected);
            Assert.IsTrue(item.SelectedIndicator.activeSelf);
        }

        [Test]
        public void HubView_StartSelectedRun_PassesSelectedMutationsToRunConfig()
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

            var stageWidget = CreateComponent<StageSelectionWidget>("StageSelectionWidget");
            var stageTitle = CreateComponent<TextMeshProUGUI>("StageTitle");
            var stageStars = CreateComponent<TextMeshProUGUI>("StageStars");
            var stageLock = new GameObject("StageLock");
            _createdObjects.Add(stageLock);
            var prevBtn = CreateComponent<Button>("PrevBtn");
            var nextBtn = CreateComponent<Button>("NextBtn");
            stageWidget.BindComponents(stageTitle, stageStars, stageLock, prevBtn, nextBtn);

            var catalog = CreateDefaultFakeCatalog();
            var mutationWidget = CreateWidget(catalog, out _);

            hubView.BindComponents(
                coinsText, playButton, optionsButton, debutButton, barracksButton, shopButton, passButton,
                stageWidget, feedbackText, mutationWidget);

            var eventBus = new EventBus();
            var stateMachine = new GameStateMachine(eventBus, GameState.Hub);
            var wallet = new Wallet(eventBus);
            var stageMapService = new StageMapService();
            var analytics = new MockAnalyticsService();
            var progressionService = new FakeProgressionService { AreMutationsUnlocked = true };

            hubView.Initialize(wallet, stageMapService, stateMachine, null, eventBus, analytics, catalog, progressionService);
            hubView.Open();

            // Select 2 mutations
            mutationWidget.ToggleMutation("armored");
            mutationWidget.ToggleMutation("last_soldier");

            Assert.AreEqual(2, hubView.SelectedMutations.Count);
            Assert.Contains("armored", (System.Collections.ICollection)hubView.SelectedMutations);
            Assert.Contains("last_soldier", (System.Collections.ICollection)hubView.SelectedMutations);

            RunStartedEvent capturedEvent = default;
            bool eventFired = false;
            eventBus.Subscribe<RunStartedEvent>(e =>
            {
                capturedEvent = e;
                eventFired = true;
            });

            bool started = hubView.StartSelectedRun();

            Assert.IsTrue(started);
            Assert.IsTrue(eventFired);
            Assert.AreEqual("stage_01", capturedEvent.LevelId);
            Assert.IsNotNull(capturedEvent.Config);
            Assert.AreEqual(2, capturedEvent.Config.MutationIds.Count);
            Assert.Contains("armored", (System.Collections.ICollection)capturedEvent.Config.MutationIds);
            Assert.Contains("last_soldier", (System.Collections.ICollection)capturedEvent.Config.MutationIds);
        }
    }
}
