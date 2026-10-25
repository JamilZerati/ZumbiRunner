using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    public class ResultViewTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private Wallet _wallet;
        private MockAnalyticsService _analytics;
        private RewardCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Boot);
            _wallet = new Wallet(_eventBus);
            _analytics = new MockAnalyticsService();
            _calculator = new RewardCalculator();
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

        private (ResultView view, ResultRewardPresenter presenter) CreateViewAndPresenter()
        {
            var view = CreateComponent<ResultView>("ResultView");
            var title = CreateComponent<TextMeshProUGUI>("TitleText");
            var coins = CreateComponent<TextMeshProUGUI>("CoinsText");
            var mult = CreateComponent<TextMeshProUGUI>("MultiplierText");
            var stars = CreateComponent<TextMeshProUGUI>("StarsText");
            var doubleBtn = CreateComponent<Button>("DoubleRewardButton");
            var continueBtn = CreateComponent<Button>("ContinueButton");

            view.BindComponents(title, coins, mult, stars, doubleBtn, continueBtn);

            var presenter = new ResultRewardPresenter(
                _calculator,
                _wallet,
                sink: null,
                stateMachine: _stateMachine,
                analytics: _analytics,
                view: view);

            return (view, presenter);
        }

        [Test]
        public void PresentResult_Victory_UpdatesUiTextsAndCalculatesBaseCoins()
        {
            var (view, presenter) = CreateViewAndPresenter();
            view.Open();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var config = new RunConfig
            {
                LevelId = "stage_01",
                RewardMultiplier = 1.0f
            };

            presenter.PresentResult(runResult, config);

            Assert.AreEqual("Vitória", view.TitleText.text);
            Assert.AreEqual("200", view.CoinsText.text);
            Assert.AreEqual("2x", view.MultiplierText.text);
            Assert.AreEqual("3", view.StarsText.text);
            Assert.IsTrue(view.DoubleRewardButton.interactable);
            Assert.IsTrue(view.ContinueButton.interactable);
            Assert.AreEqual(200, presenter.TotalCoins);
            Assert.IsFalse(presenter.IsDoubled);
            Assert.IsFalse(presenter.IsClaimed);
        }

        [Test]
        public void PresentResult_Defeat_ShowsDefeatTitleAndHandlesStats()
        {
            var (view, presenter) = CreateViewAndPresenter();
            view.Open();

            var runResult = new RunResult
            {
                LevelId = "stage_02",
                Victory = false,
                CoinsEarned = 50,
                MultiplierReached = 1,
                Stars = 0
            };
            var config = new RunConfig { LevelId = "stage_02" };

            presenter.PresentResult(runResult, config);

            Assert.AreEqual("Derrota", view.TitleText.text);
            Assert.AreEqual("50", view.CoinsText.text);
            Assert.AreEqual("0", view.StarsText.text);
        }

        [Test]
        public void PresentResult_TracksRunEndAndAdOfferAnalytics()
        {
            var (view, presenter) = CreateViewAndPresenter();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);

            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.RunEnd));
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdOffer));
        }

        [Test]
        public void DoubleReward_RecalculatesRewardWithMultiplier_UpdatesUi_AndTracksAnalytics()
        {
            var (view, presenter) = CreateViewAndPresenter();
            view.Open();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);
            Assert.AreEqual(200, presenter.TotalCoins);

            view.DoubleRewardButton.onClick.Invoke();

            Assert.IsTrue(presenter.IsDoubled);
            Assert.AreEqual(400, presenter.TotalCoins);
            Assert.AreEqual("400", view.CoinsText.text);
            Assert.AreEqual("4x", view.MultiplierText.text);
            Assert.IsFalse(view.DoubleRewardButton.interactable);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdReward));
            Assert.IsTrue(_analytics.HasEvent("ad_reward_claimed"));
        }

        [Test]
        public void DoubleReward_CalledTwice_SecondCallIsIgnored()
        {
            var (_, presenter) = CreateViewAndPresenter();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);
            Assert.IsTrue(presenter.DoubleReward());
            Assert.IsFalse(presenter.DoubleReward());
            Assert.AreEqual(400, presenter.TotalCoins);
        }

        [Test]
        public void ClaimAndContinue_GrantsCoinsToWallet_TransitionsToHub_AndClosesWindow()
        {
            var (view, presenter) = CreateViewAndPresenter();
            view.Open();
            Assert.IsTrue(view.IsOpen);

            _stateMachine.TryTransition(GameState.Run);
            _stateMachine.TryTransition(GameState.Result);
            Assert.AreEqual(GameState.Result, _stateMachine.CurrentState);

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);
            presenter.DoubleReward();

            view.ContinueButton.onClick.Invoke();

            Assert.IsTrue(presenter.IsClaimed);
            Assert.IsFalse(view.IsOpen);
            Assert.AreEqual(400, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
            Assert.IsFalse(view.ContinueButton.interactable);
            Assert.IsFalse(view.DoubleRewardButton.interactable);
        }

        [Test]
        public void ClaimAndContinue_CalledMultipleTimes_IsIdempotentAndGuardsWallet()
        {
            var (_, presenter) = CreateViewAndPresenter();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 1,
                Stars = 1
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);

            Assert.IsTrue(presenter.ClaimAndContinue());
            Assert.AreEqual(100, _wallet.GetBalance(CurrencyKind.Coins));

            Assert.IsFalse(presenter.ClaimAndContinue());
            Assert.AreEqual(100, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void DoubleReward_AfterClaimAndContinue_IsRejected()
        {
            var (_, presenter) = CreateViewAndPresenter();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                CoinsEarned = 100,
                MultiplierReached = 1,
                Stars = 1
            };
            var config = new RunConfig { LevelId = "stage_01" };

            presenter.PresentResult(runResult, config);
            Assert.IsTrue(presenter.ClaimAndContinue());

            Assert.IsFalse(presenter.DoubleReward());
            Assert.AreEqual(100, presenter.TotalCoins);
            Assert.AreEqual(100, _wallet.GetBalance(CurrencyKind.Coins));
        }

        [Test]
        public void StandalonePresenter_WithoutView_CalculatesAndGrantsCorrectly()
        {
            var presenter = new ResultRewardPresenter(
                _calculator,
                _wallet,
                sink: null,
                stateMachine: _stateMachine,
                analytics: _analytics);

            var runResult = new RunResult
            {
                LevelId = "stage_03",
                Victory = true,
                CoinsEarned = 150,
                MultiplierReached = 3,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_03", RewardMultiplier = 1.0f };

            presenter.PresentResult(runResult, config);
            Assert.AreEqual(450, presenter.TotalCoins);

            Assert.IsTrue(presenter.DoubleReward());
            Assert.AreEqual(900, presenter.TotalCoins);

            Assert.IsTrue(presenter.ClaimAndContinue());
            Assert.AreEqual(900, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(GameState.Hub, _stateMachine.CurrentState);
        }

        [Test]
        public void ResultView_RegisteredInUIManager_BehavesAsModalWindow()
        {
            var uiManager = CreateComponent<UIManager>("UIManager");
            uiManager.Initialize(_stateMachine, _analytics, _eventBus);

            var modalLayer = CreateComponent<UILayer>("ModalLayer");
            modalLayer.Initialize(UILayerType.Modal);
            uiManager.RegisterLayer(modalLayer);

            var (view, presenter) = CreateViewAndPresenter();
            uiManager.RegisterWindow(view);

            Assert.AreEqual(UILayerType.Modal, view.LayerType);
            Assert.IsFalse(uiManager.IsModalOpen());

            uiManager.OpenWindow(view);
            Assert.IsTrue(view.IsOpen);
            Assert.IsTrue(uiManager.IsModalOpen());

            var runResult = new RunResult { LevelId = "stage_01", CoinsEarned = 50, MultiplierReached = 1 };
            presenter.PresentResult(runResult, new RunConfig());
            presenter.ClaimAndContinue();

            Assert.IsFalse(view.IsOpen);
            Assert.IsFalse(uiManager.IsModalOpen());
        }
    }
}
