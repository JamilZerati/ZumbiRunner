using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Meta.Barracks;
using Game.Meta.Time;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using Game.Presentation.UI.Barracks;
using Game.Tests.EditMode.Meta.Doubles;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.UI
{
    public class BarracksViewTests
    {
        private readonly List<GameObject> _createdObjects = new();

        private EventBus _eventBus;
        private FakeTimeProvider _timeProvider;
        private Wallet _wallet;
        private BarracksCatalog _catalog;
        private BarracksService _service;

        private BarracksView _barracksView;
        private Button _closeButton;
        private CanteenWidget _canteenWidget;
        private List<BarracksBuildingCardWidget> _cardWidgets;
        private TMP_Text _clockWarningText;
        private TMP_Text _coinsText;
        private TMP_Text _recruitsText;
        private TMP_Text _medalsText;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _timeProvider = new FakeTimeProvider(new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc));
            _wallet = new Wallet(_eventBus);
            _catalog = BarracksCatalog.CreateDefault();
            _service = new BarracksService(_timeProvider, _wallet, _catalog, eventBus: _eventBus);

            _barracksView = CreateComponent<BarracksView>("BarracksView");
            _closeButton = CreateComponent<Button>("CloseButton");
            _clockWarningText = CreateComponent<TextMeshProUGUI>("ClockWarningText");
            _coinsText = CreateComponent<TextMeshProUGUI>("CoinsText");
            _recruitsText = CreateComponent<TextMeshProUGUI>("RecruitsText");
            _medalsText = CreateComponent<TextMeshProUGUI>("MedalsText");

            _canteenWidget = CreateComponent<CanteenWidget>("CanteenWidget");
            var pendingCoins = CreateComponent<TextMeshProUGUI>("PendingCoins");
            var collectBtn = CreateComponent<Button>("CollectBtn");
            var doubleAdBtn = CreateComponent<Button>("DoubleAdBtn");
            _canteenWidget.BindComponents(pendingCoins, collectBtn, doubleAdBtn, _clockWarningText);

            _cardWidgets = new List<BarracksBuildingCardWidget>();
            var buildingTypes = new[]
            {
                BarracksBuildingType.Recruitment,
                BarracksBuildingType.Armory,
                BarracksBuildingType.Canteen,
                BarracksBuildingType.Infirmary,
                BarracksBuildingType.Ammunition
            };

            foreach (var bType in buildingTypes)
            {
                var card = CreateComponent<BarracksBuildingCardWidget>($"Card_{bType}");
                var title = CreateComponent<TextMeshProUGUI>("Title");
                var level = CreateComponent<TextMeshProUGUI>("Level");
                var coinCost = CreateComponent<TextMeshProUGUI>("CoinCost");
                var recruitCost = CreateComponent<TextMeshProUGUI>("RecruitCost");
                var timer = CreateComponent<TextMeshProUGUI>("Timer");
                var upgradeBtn = CreateComponent<Button>("UpgradeBtn");
                var speedupBtn = CreateComponent<Button>("SpeedupBtn");
                var instantBtn = CreateComponent<Button>("InstantBtn");
                var medalCost = CreateComponent<TextMeshProUGUI>("MedalCost");

                card.BindComponents(title, level, coinCost, recruitCost, timer, upgradeBtn, speedupBtn, instantBtn, medalCost, bType);
                _cardWidgets.Add(card);
            }

            _barracksView.BindComponents(_closeButton, _canteenWidget, _cardWidgets, _clockWarningText, null, _coinsText, _recruitsText, _medalsText);
            _barracksView.Initialize(_service, _catalog, _wallet, _eventBus);
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
        public void LayerType_IsModal()
        {
            Assert.AreEqual(UILayerType.Modal, _barracksView.LayerType);
        }

        [Test]
        public void Initialize_CardsDisplayInitialLevelsAndCosts()
        {
            var recruitmentCard = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            Assert.IsNotNull(recruitmentCard);
            Assert.AreEqual("Recrutamento", recruitmentCard.TitleText.text);
            Assert.AreEqual("Nível 1", recruitmentCard.LevelText.text);
            Assert.AreEqual("200", recruitmentCard.CoinCostText.text);
            Assert.AreEqual("10", recruitmentCard.RecruitCostText.text);
        }

        [Test]
        public void OpenWindow_RefreshesBalancesAndDisplaysAmounts()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "test");
            _wallet.Add(CurrencyKind.Recruits, 25, "test");
            _wallet.Add(CurrencyKind.Medals, 10, "test");

            _barracksView.OpenWindow();

            Assert.IsTrue(_barracksView.IsOpen);
            Assert.AreEqual("500", _barracksView.CoinsText.text);
            Assert.AreEqual("25", _barracksView.RecruitsText.text);
            Assert.AreEqual("10", _barracksView.MedalsText.text);
        }

        [Test]
        public void UpgradeButton_DisabledWhenInsufficientFunds_EnabledWhenSufficient()
        {
            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            Assert.IsFalse(card.UpgradeButton.interactable);

            _wallet.Add(CurrencyKind.Coins, 200, "test");
            _wallet.Add(CurrencyKind.Recruits, 10, "test");
            _barracksView.RefreshView();

            Assert.IsTrue(card.UpgradeButton.interactable);
        }

        [Test]
        public void WalletChangedEvent_DynamicallyRefreshesButtons()
        {
            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            Assert.IsFalse(card.UpgradeButton.interactable);

            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            Assert.IsTrue(card.UpgradeButton.interactable);
        }

        [Test]
        public void UnderConstruction_DisplaysTimerAndInstantMedalOption()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            card.UpgradeButton.onClick.Invoke();

            Assert.IsTrue(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            Assert.IsFalse(card.UpgradeButton.interactable);
            Assert.IsTrue(card.SpeedupAdButton.gameObject.activeSelf);
            Assert.IsTrue(card.InstantMedalButton.gameObject.activeSelf);
            Assert.IsTrue(card.TimerText.text.Length > 0);
            Assert.IsTrue(card.InstantMedalCostText.text.Contains("Medalhas"));
        }

        [Test]
        public void InstantMedalButton_CompletesConstructionImmediately()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");
            _wallet.Add(CurrencyKind.Medals, 100, "grant");

            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            card.UpgradeButton.onClick.Invoke();
            Assert.AreEqual(1, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));

            card.InstantMedalButton.onClick.Invoke();

            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            Assert.AreEqual(2, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual("Nível 2", card.LevelText.text);
        }

        [Test]
        public void SpeedupAdButton_WhenClicked_ReducesTimer()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            card.UpgradeButton.onClick.Invoke();

            var initialRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            card.SpeedupAdButton.onClick.Invoke();

            var afterRemaining = _service.GetRemainingConstructionTime(BarracksBuildingType.Recruitment);
            Assert.AreEqual(initialRemaining - TimeSpan.FromMinutes(30), afterRemaining);
        }

        [Test]
        public void UpgradeButton_WhenTimerExpired_CompletesUpgradeAndPromotesLevel()
        {
            _wallet.Add(CurrencyKind.Coins, 500, "grant");
            _wallet.Add(CurrencyKind.Recruits, 50, "grant");

            var card = _barracksView.GetCard(BarracksBuildingType.Recruitment);
            card.UpgradeButton.onClick.Invoke();

            _timeProvider.Advance(TimeSpan.FromHours(5));
            _barracksView.RefreshView();

            card.UpgradeButton.onClick.Invoke();

            Assert.IsFalse(_service.IsUnderConstruction(BarracksBuildingType.Recruitment));
            Assert.AreEqual(2, _service.GetBuildingLevel(BarracksBuildingType.Recruitment));
            Assert.AreEqual("Nível 2", card.LevelText.text);
        }

        [Test]
        public void CanteenWidget_DisplaysPendingCoins_AndCollects()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _barracksView.RefreshView();

            int pending = _service.GetPendingOfflineCoins();
            Assert.Greater(pending, 0);
            Assert.AreEqual(pending.ToString(), _canteenWidget.PendingCoinsText.text);
            Assert.IsTrue(_canteenWidget.CollectButton.interactable);

            _canteenWidget.CollectButton.onClick.Invoke();

            Assert.AreEqual(pending, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());
            Assert.AreEqual("0", _canteenWidget.PendingCoinsText.text);
            Assert.IsFalse(_canteenWidget.CollectButton.interactable);
        }

        [Test]
        public void CanteenWidget_DoubleWithAd_CreditsDoubleCoins()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _barracksView.RefreshView();

            int pending = _service.GetPendingOfflineCoins();
            Assert.Greater(pending, 0);

            _canteenWidget.DoubleAdButton.onClick.Invoke();

            Assert.AreEqual(pending * 2, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(0, _service.GetPendingOfflineCoins());
        }

        [Test]
        public void ClockManipulation_ShowsWarningAndDisablesInteractions()
        {
            _timeProvider.Advance(TimeSpan.FromHours(2));
            _barracksView.RefreshView();
            Assert.IsTrue(_canteenWidget.CollectButton.interactable);

            _timeProvider.SetManipulated(true);
            _barracksView.RefreshView();

            Assert.IsTrue(_clockWarningText.gameObject.activeSelf);
            Assert.IsFalse(_canteenWidget.CollectButton.interactable);
            Assert.IsFalse(_canteenWidget.DoubleAdButton.interactable);
        }

        [Test]
        public void CloseButton_ClosesWindowAndInvokesEvent()
        {
            bool closeClicked = false;
            _barracksView.OnCloseClicked += () => closeClicked = true;

            _barracksView.OpenWindow();
            Assert.IsTrue(_barracksView.IsOpen);

            _barracksView.CloseButton.onClick.Invoke();

            Assert.IsFalse(_barracksView.IsOpen);
            Assert.IsTrue(closeClicked);
        }
    }
}
