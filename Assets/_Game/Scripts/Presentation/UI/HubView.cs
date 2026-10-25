using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class HubView : UIWindow
    {
        [SerializeField] private TMP_Text _coinsText, _navigationFeedbackText;
        [SerializeField] private Button _playButton, _optionsButton, _debutJourneyButton, _barracksButton, _shopButton, _passButton;
        [SerializeField] private StageSelectionWidget _stageSelectionWidget;

        private IWallet _wallet;
        private IStageMapService _stageMapService;
        private IGameStateMachine _stateMachine;
        private UIManager _uiManager;
        private IEventBus _eventBus;
        private IAnalyticsService _analytics;
        private IDisposable _walletSubscription;
        private string _fallbackSelectedStageId;

        public override UILayerType LayerType => UILayerType.Hub;
        public TMP_Text CoinsText => _coinsText;
        public Button PlayButton => _playButton;
        public Button OptionsButton => _optionsButton;
        public Button DebutJourneyButton => _debutJourneyButton;
        public Button BarracksButton => _barracksButton;
        public Button ShopButton => _shopButton;
        public Button PassButton => _passButton;
        public StageSelectionWidget StageSelectionWidget => _stageSelectionWidget;
        public TMP_Text NavigationFeedbackText => _navigationFeedbackText;
        public string LastNavigationShortcut { get; private set; }
        public Action DebutJourneyHook { get; set; }

        public event Action<string> OnRunStarted;
        public event Action OnOptionsRequested;
        public event Action OnDebutJourneyRequested;
        public event Action<string> OnNavigationShortcutClicked;

        public string SelectedStageId => _stageSelectionWidget != null
            ? _stageSelectionWidget.SelectedStageId
            : (!string.IsNullOrEmpty(_fallbackSelectedStageId) ? _fallbackSelectedStageId : _stageMapService?.GetHighestUnlockedStage() ?? "stage_01");

        protected override void Awake()
        {
            SetLayerType(UILayerType.Hub);
            base.Awake();
            if (_stageSelectionWidget == null) _stageSelectionWidget = GetComponentInChildren<StageSelectionWidget>(true);
            WireButtons();
        }

        protected override void OnDestroy()
        {
            _walletSubscription?.Dispose();
            _walletSubscription = null;
            UnwireButtons();
            OnRunStarted = null;
            OnOptionsRequested = null;
            OnDebutJourneyRequested = null;
            OnNavigationShortcutClicked = null;
            DebutJourneyHook = null;
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshCoins();
            _stageSelectionWidget?.Refresh();
        }

        public void BindComponents(TMP_Text coins = null, Button play = null, Button options = null, Button debut = null, Button barracks = null, Button shop = null, Button pass = null, StageSelectionWidget stageWidget = null, TMP_Text feedback = null)
        {
            UnwireButtons();
            if (coins != null) _coinsText = coins;
            if (play != null) _playButton = play;
            if (options != null) _optionsButton = options;
            if (debut != null) _debutJourneyButton = debut;
            if (barracks != null) _barracksButton = barracks;
            if (shop != null) _shopButton = shop;
            if (pass != null) _passButton = pass;
            if (stageWidget != null) _stageSelectionWidget = stageWidget;
            if (feedback != null) _navigationFeedbackText = feedback;
            WireButtons();
            RefreshCoins();
            _stageSelectionWidget?.Refresh();
        }

        public void Initialize(IWallet wallet, IStageMapService stageMapService, IGameStateMachine stateMachine = null, UIManager uiManager = null, IEventBus eventBus = null, IAnalyticsService analytics = null)
        {
            _walletSubscription?.Dispose();
            _wallet = wallet;
            _stageMapService = stageMapService;
            _stateMachine = stateMachine;
            _uiManager = uiManager;
            _eventBus = eventBus;
            _analytics = analytics;

            if (_eventBus != null) _walletSubscription = _eventBus.Subscribe<WalletChangedEvent>(OnWalletChanged);
            if (_stageSelectionWidget != null && stageMapService != null) _stageSelectionWidget.Initialize(stageMapService);

            WireButtons();
            RefreshCoins();
        }

        public bool SelectStage(string stageId)
        {
            if (_stageSelectionWidget != null) return _stageSelectionWidget.SelectStage(stageId);
            if (string.IsNullOrWhiteSpace(stageId) || (_stageMapService != null && !_stageMapService.IsStageUnlocked(stageId))) return false;
            _fallbackSelectedStageId = stageId;
            return true;
        }

        public bool StartSelectedRun()
        {
            string stageId = SelectedStageId;
            if (string.IsNullOrWhiteSpace(stageId) || (_stageMapService != null && !_stageMapService.IsStageUnlocked(stageId))) return false;

            bool ok = _stateMachine != null ? _stateMachine.TryTransition(GameState.Run) : (_uiManager != null && _uiManager.TryTransition(GameState.Run));
            if (ok)
            {
                _eventBus?.Publish(new RunStartedEvent(stageId, new RunConfig { LevelId = stageId }));
                OnRunStarted?.Invoke(stageId);
                return true;
            }
            return false;
        }

        public void OpenOptions() { OnOptionsRequested?.Invoke(); _uiManager?.OpenWindow<OptionsView>(); }
        public void OpenDebutJourney() { OnDebutJourneyRequested?.Invoke(); DebutJourneyHook?.Invoke(); }

        public void NavigateToShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut)) return;
            LastNavigationShortcut = shortcut;
            if (_navigationFeedbackText != null) _navigationFeedbackText.text = shortcut;
            OnNavigationShortcutClicked?.Invoke(shortcut);
            _analytics?.TrackEvent("button_click", new Dictionary<string, object>
            {
                ["button_id"] = $"hub_shortcut_{shortcut.ToLowerInvariant()}",
                ["screen_name"] = nameof(HubView)
            });
        }

        public void RefreshCoins() { if (_coinsText != null) _coinsText.text = (_wallet?.GetBalance(CurrencyKind.Coins) ?? 0).ToString(); }
        private void OnWalletChanged(WalletChangedEvent evt) { if (evt.Currency == CurrencyKind.Coins && _coinsText != null) _coinsText.text = evt.NewBalance.ToString(); }

        private void WireButtons()
        {
            UnwireButtons();
            _playButton?.onClick.AddListener(() => StartSelectedRun());
            _optionsButton?.onClick.AddListener(OpenOptions);
            _debutJourneyButton?.onClick.AddListener(OpenDebutJourney);
            _barracksButton?.onClick.AddListener(() => NavigateToShortcut("Quartel"));
            _shopButton?.onClick.AddListener(() => NavigateToShortcut("Loja"));
            _passButton?.onClick.AddListener(() => NavigateToShortcut("Passe"));
        }

        private void UnwireButtons()
        {
            _playButton?.onClick.RemoveAllListeners();
            _optionsButton?.onClick.RemoveAllListeners();
            _debutJourneyButton?.onClick.RemoveAllListeners();
            _barracksButton?.onClick.RemoveAllListeners();
            _shopButton?.onClick.RemoveAllListeners();
            _passButton?.onClick.RemoveAllListeners();
        }
    }
}
