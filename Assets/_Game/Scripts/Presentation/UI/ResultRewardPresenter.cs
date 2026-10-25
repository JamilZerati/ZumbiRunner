using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Wallet;

namespace Game.Presentation.UI
{
    public class ResultRewardPresenter
    {
        private readonly IRewardCalculator _calculator;
        private readonly IWallet _wallet;
        private readonly IRewardSink _sink;
        private readonly IGameStateMachine _stateMachine;
        private readonly IAnalyticsService _analytics;
        private ResultView _view;

        private RunResult _currentResult;
        private RunConfig _currentConfig;
        private RewardBundle _currentBundle;
        private int _totalCoins;
        private float _multiplier;
        private bool _isDoubled;
        private bool _isClaimed;

        public RunResult CurrentResult => _currentResult;
        public RunConfig CurrentConfig => _currentConfig;
        public RewardBundle CurrentBundle => _currentBundle;
        public int TotalCoins => _totalCoins;
        public float Multiplier => _multiplier;
        public bool IsDoubled => _isDoubled;
        public bool IsClaimed => _isClaimed;

        public ResultRewardPresenter(
            IRewardCalculator calculator = null,
            IWallet wallet = null,
            IRewardSink sink = null,
            IGameStateMachine stateMachine = null,
            IAnalyticsService analytics = null,
            ResultView view = null)
        {
            _calculator = calculator ?? new RewardCalculator();
            _wallet = wallet ?? sink?.Wallet;
            _sink = sink ?? (_wallet != null ? new RewardSink(_wallet) : null);
            _stateMachine = stateMachine;
            _analytics = analytics;
            if (view != null) AttachView(view);
        }

        public ResultRewardPresenter(
            IRewardCalculator calculator,
            IRewardSink sink,
            IGameStateMachine stateMachine = null,
            IAnalyticsService analytics = null,
            ResultView view = null)
            : this(calculator, sink?.Wallet, sink, stateMachine, analytics, view) { }

        public void AttachView(ResultView view)
        {
            _view = view;
            _view?.Initialize(this);
        }

        public void PresentResult(RunResult result, RunConfig config)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));

            _currentResult = result;
            _currentConfig = config;
            _isDoubled = false;
            _isClaimed = false;

            _currentBundle = _calculator.Calculate(result, config, doubleReward: false);
            _totalCoins = ExtractCoinsFromBundle(_currentBundle);
            _multiplier = result.MultiplierReached > 0 ? result.MultiplierReached : 1.0f;

            _analytics?.TrackEvent(AnalyticsEvents.RunEnd, new Dictionary<string, object>
            {
                ["level_id"] = result.LevelId ?? string.Empty,
                ["victory"] = result.Victory,
                ["stars"] = result.Stars,
                ["coins"] = _totalCoins
            });

            _analytics?.TrackEvent(AnalyticsEvents.AdOffer, new Dictionary<string, object>
            {
                ["offer_type"] = "double_reward",
                ["placement"] = "result_screen"
            });

            _view?.RefreshView(this);
        }

        public bool DoubleReward()
        {
            if (_isClaimed || _isDoubled || _currentResult == null) return false;

            _isDoubled = true;
            _currentBundle = _calculator.Calculate(_currentResult, _currentConfig, doubleReward: true);
            _totalCoins = ExtractCoinsFromBundle(_currentBundle);

            _analytics?.TrackEvent(AnalyticsEvents.AdReward, new Dictionary<string, object>
            {
                ["reward_type"] = "double_coins",
                ["coins"] = _totalCoins
            });

            _analytics?.TrackEvent("ad_reward_claimed", new Dictionary<string, object>
            {
                ["coins"] = _totalCoins
            });

            _view?.RefreshView(this);
            return true;
        }

        public bool ClaimAndContinue()
        {
            if (_isClaimed) return false;

            _isClaimed = true;
            _view?.SetButtonsInteractable(false);

            bool granted = _currentBundle != null && _sink != null && _currentBundle.Grant(_sink);
            _stateMachine?.TryTransition(GameState.Hub);
            _view?.Close();

            return granted;
        }

        private static int ExtractCoinsFromBundle(RewardBundle bundle)
        {
            if (bundle?.Rewards == null) return 0;
            for (int i = 0; i < bundle.Rewards.Count; i++)
            {
                if (bundle.Rewards[i] is CurrencyReward currencyReward && currencyReward.Currency == CurrencyKind.Coins)
                {
                    return currencyReward.Amount;
                }
            }
            return 0;
        }
    }
}
