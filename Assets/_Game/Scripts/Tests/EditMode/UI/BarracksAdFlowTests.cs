using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Meta.Ads;
using Game.Meta.Barracks;
using Game.Meta.Time;
using Game.Tests.EditMode.Meta.Doubles;
using Game.Meta.Wallet;
using Game.Presentation.UI.Barracks;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.UI
{
    public class BarracksAdFlowTests
    {
        private readonly List<GameObject> _createdObjects = new();

        private EventBus _eventBus;
        private FakeTimeProvider _timeProvider;
        private Wallet _wallet;
        private BarracksCatalog _catalog;
        private BarracksService _service;
        private MockAnalyticsService _analytics;
        private MockAdService _adService;
        private AdPolicy _adPolicy;

        private CanteenWidget _canteenWidget;
        private BarracksBuildingCardWidget _cardWidget;
        private TMP_Text _clockWarningText;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc));
            _wallet = new Wallet(_eventBus);
            _catalog = BarracksCatalog.CreateDefault();
            _service = new BarracksService(_timeProvider, _wallet, _catalog, eventBus: _eventBus);
            _analytics = new MockAnalyticsService();
            _adService = new MockAdService(_analytics);
            _adPolicy = new AdPolicy(_timeProvider, entitlements: null);

            _clockWarningText = CreateComponent<TextMeshProUGUI>("ClockWarningText");

            _canteenWidget = CreateComponent<CanteenWidget>("CanteenWidget");
            var pendingCoins = CreateComponent<TextMeshProUGUI>("PendingCoins");
            var collectBtn = CreateComponent<Button>("CollectBtn");
            var doubleAdBtn = CreateComponent<Button>("DoubleAdBtn");
            _canteenWidget.BindComponents(pendingCoins, collectBtn, doubleAdBtn, _clockWarningText);
            _canteenWidget.Initialize(_service, _wallet, _adService, _adPolicy);

            _cardWidget = CreateComponent<BarracksBuildingCardWidget>("Card_Recruitment");
            var title = CreateComponent<TextMeshProUGUI>("Title");
            var level = CreateComponent<TextMeshProUGUI>("Level");
            var coinCost = CreateComponent<TextMeshProUGUI>("CoinCost");
            var recruitCost = CreateComponent<TextMeshProUGUI>("RecruitCost");
            var timer = CreateComponent<TextMeshProUGUI>("Timer");
            var upgradeBtn = CreateComponent<Button>("UpgradeBtn");
            var speedupBtn = CreateComponent<Button>("SpeedupBtn");
            var instantBtn = CreateComponent<Button>("InstantBtn");
            var medalCost = CreateComponent<TextMeshProUGUI>("MedalCost");

            _cardWidget.BindComponents(title, level, coinCost, recruitCost, timer, upgradeBtn, speedupBtn, instantBtn, medalCost, BarracksBuildingType.Recruitment);
            var def = _catalog.Get(BarracksBuildingType.Recruitment);
            _cardWidget.Initialize(BarracksBuildingType.Recruitment, _service, def, _wallet, _adService, _adPolicy);
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
        public void CT13_CanteenWidget_WhenAdCompleted_DoublesOfflineCoinsCollection()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _canteenWidget.Refresh();

            int pending = _service.GetPendingOfflineCoins();
            Assert.Greater(pending, 0);
            Assert.IsTrue(_canteenWidget.DoubleAdButton.interactable);

            _adService.SetPlacementResult(AdPlacement.CanteenDouble, AdShowResult.Completed);
            _canteenWidget.DoubleAdButton.onClick.Invoke();

            Assert.AreEqual(pending * 2, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());
            Assert.IsTrue(_adService.HasShown(AdPlacement.CanteenDouble));
            Assert.Greater(_adPolicy.State.LastAdShownUtcTicks, 0);
        }

        [Test]
        public void CanteenWidget_WhenAdFailedOrSkipped_DoesNotCollectCoins_AndPreservesPending()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _canteenWidget.Refresh();

            int pending = _service.GetPendingOfflineCoins();
            Assert.Greater(pending, 0);

            _adService.SetPlacementResult(AdPlacement.CanteenDouble, AdShowResult.Failed);
            _canteenWidget.DoubleAdButton.onClick.Invoke();

            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(pending, _service.GetPendingOfflineCoins());
            Assert.IsTrue(_canteenWidget.CollectButton.interactable);
            Assert.AreEqual(0, _adPolicy.State.LastAdShownUtcTicks);
        }

        [Test]
        public void CanteenWidget_WhenAdPolicyDisallowsRewarded_DoubleAdButtonIsDisabled()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _adPolicy.State.DailyRewardedCount = _adPolicy.Definition.DailyRewardedLimit;

            _canteenWidget.Refresh();

            Assert.IsFalse(_canteenWidget.DoubleAdButton.interactable);
            Assert.IsTrue(_canteenWidget.CollectButton.interactable);
        }

        [Test]
        public void CT14_BarracksBuildingCardWidget_WhenAdCompleted_ConsumesSpeedupQuota_AndReducesConstructionTimerBy30Minutes()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            _cardWidget.Refresh(_wallet);
            _cardWidget.UpgradeButton.onClick.Invoke();

            Assert.IsTrue(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            var initialRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);

            _adService.SetPlacementResult(AdPlacement.BarracksSpeedup, AdShowResult.Completed);
            _cardWidget.SpeedupAdButton.onClick.Invoke();

            var afterRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining - TimeSpan.FromMinutes(30), afterRemaining);
            Assert.IsTrue(_adService.HasShown(AdPlacement.BarracksSpeedup));
            Assert.Greater(_adPolicy.State.LastAdShownUtcTicks, 0);
        }

        [Test]
        public void BarracksBuildingCardWidget_WhenAdFailedOrSkipped_DoesNotReduceTimer_AndDoesNotConsumeQuota()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            _cardWidget.Refresh(_wallet);
            _cardWidget.UpgradeButton.onClick.Invoke();

            var initialRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);

            _adService.SetPlacementResult(AdPlacement.BarracksSpeedup, AdShowResult.Skipped);
            _cardWidget.SpeedupAdButton.onClick.Invoke();

            var afterRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining, afterRemaining);
            Assert.AreEqual(0, _adPolicy.State.LastAdShownUtcTicks);
        }

        [Test]
        public void BarracksBuildingCardWidget_WhenSpeedupDailyLimitReached_SpeedupAdButtonIsDisabled()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            _cardWidget.Refresh(_wallet);
            _cardWidget.UpgradeButton.onClick.Invoke();

            _adService.SetPlacementResult(AdPlacement.BarracksSpeedup, AdShowResult.Completed);

            // Execute 3 speedups to hit the daily limit
            _cardWidget.SpeedupAdButton.onClick.Invoke();
            _cardWidget.SpeedupAdButton.onClick.Invoke();
            _cardWidget.SpeedupAdButton.onClick.Invoke();

            _cardWidget.Refresh(_wallet);
            Assert.IsFalse(_cardWidget.SpeedupAdButton.interactable);
        }
    }
}
