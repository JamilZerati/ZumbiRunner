using System.Collections.Generic;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.State;
using Game.Meta.Ads;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Time;
using Game.Tests.EditMode.Meta.Doubles;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Tests.EditMode.UI
{
    public class ResultRewardAdTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private EventBus _eventBus;
        private GameStateMachine _stateMachine;
        private Wallet _wallet;
        private MockAnalyticsService _analytics;
        private RewardCalculator _calculator;
        private MockAdService _adService;
        private FakeTimeProvider _timeProvider;
        private AdPolicy _adPolicy;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _stateMachine = new GameStateMachine(_eventBus, GameState.Boot);
            _wallet = new Wallet(_eventBus);
            _analytics = new MockAnalyticsService();
            _calculator = new RewardCalculator();
            _adService = new MockAdService(_analytics);
            _timeProvider = new FakeTimeProvider(new System.DateTime(2026, 11, 10, 12, 0, 0, System.DateTimeKind.Utc));
            _adPolicy = new AdPolicy(_timeProvider, entitlements: null);
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

        private (ResultView view, ResultRewardPresenter presenter) CreateViewAndPresenter(
            IAdService adService = null,
            IAdPolicy adPolicy = null)
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
                view: view,
                adService: adService ?? _adService,
                adPolicy: adPolicy ?? _adPolicy);

            return (view, presenter);
        }

        [Test]
        public void CT11_DoubleReward_WhenAdCompleted_DoublesCoins_UpdatesUi_AndEmitsAnalytics()
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

            _adService.SetPlacementResult(AdPlacement.DoubleReward, AdShowResult.Completed);
            presenter.PresentResult(runResult, config);

            Assert.AreEqual(200, presenter.TotalCoins);
            Assert.IsTrue(view.DoubleRewardButton.interactable);

            view.DoubleRewardButton.onClick.Invoke();

            Assert.IsTrue(presenter.IsDoubled);
            Assert.AreEqual(400, presenter.TotalCoins);
            Assert.AreEqual("400", view.CoinsText.text);
            Assert.AreEqual("4x", view.MultiplierText.text);
            Assert.IsFalse(view.DoubleRewardButton.interactable);
            Assert.IsTrue(_adService.HasShown(AdPlacement.DoubleReward));
            Assert.Greater(_adPolicy.State.LastAdShownUtcTicks, 0);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdReward));
            Assert.IsTrue(_analytics.HasEvent("ad_reward_claimed"));

            view.ContinueButton.onClick.Invoke();
            Assert.AreEqual(400, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.IsTrue(presenter.IsClaimed);
        }

        [Test]
        public void CT12_DoubleReward_WhenAdSkippedOrFailed_PreservesBaseCoins_AndAllowsClaimingBaseReward()
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

            _adService.SetPlacementResult(AdPlacement.DoubleReward, AdShowResult.Skipped);
            presenter.PresentResult(runResult, config);

            Assert.AreEqual(200, presenter.TotalCoins);
            Assert.IsTrue(view.DoubleRewardButton.interactable);

            view.DoubleRewardButton.onClick.Invoke();

            Assert.IsFalse(presenter.IsDoubled);
            Assert.AreEqual(200, presenter.TotalCoins);
            Assert.AreEqual("200", view.CoinsText.text);
            Assert.AreEqual("2x", view.MultiplierText.text);
            Assert.IsTrue(view.ContinueButton.interactable);
            Assert.AreEqual(0, _adPolicy.State.LastAdShownUtcTicks);
            Assert.IsTrue(_analytics.HasEvent(AnalyticsEvents.AdFailed));

            view.ContinueButton.onClick.Invoke();
            Assert.AreEqual(200, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.IsTrue(presenter.IsClaimed);
        }

        [Test]
        public void DoubleReward_WhenAdNotAvailable_ButtonIsDisabled_AndDoubleRewardReturnsFalse()
        {
            _adService.SetPlacementAvailable(AdPlacement.DoubleReward, false);
            var (view, presenter) = CreateViewAndPresenter();
            view.Open();

            var runResult = new RunResult { LevelId = "stage_01", CoinsEarned = 50, MultiplierReached = 1 };
            presenter.PresentResult(runResult, new RunConfig());

            Assert.IsFalse(view.DoubleRewardButton.interactable);
            Assert.IsFalse(presenter.DoubleReward());
            Assert.IsFalse(presenter.IsDoubled);
            Assert.AreEqual(50, presenter.TotalCoins);
        }

        [Test]
        public void DoubleReward_WhenPolicyDisallowsRewarded_ButtonIsDisabled()
        {
            _adPolicy.State.DailyRewardedCount = _adPolicy.Definition.DailyRewardedLimit;

            var (view, presenter) = CreateViewAndPresenter();
            view.Open();

            var runResult = new RunResult { LevelId = "stage_01", CoinsEarned = 50, MultiplierReached = 1 };
            presenter.PresentResult(runResult, new RunConfig());

            Assert.IsFalse(view.DoubleRewardButton.interactable);
            Assert.IsFalse(presenter.DoubleReward());
        }
    }
}
