using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.Events;
using Game.Core.State;
using Game.Data.Mutations;
using Game.Meta.Ads;
using Game.Meta.Progression;
using Game.Meta.Run;
using Game.Meta.Stages;
using Game.Meta.Wallet;
using Game.Presentation.UI.Barracks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class HubView : UIWindow
    {
        [SerializeField] private TMP_Text _coinsText, _navigationFeedbackText;
        [SerializeField] private Button _playButton, _optionsButton, _debutJourneyButton, _barracksButton, _shopButton, _passButton, _boostSquadButton;
        [SerializeField] private TMP_Text _boostSquadStatusText;
        [SerializeField] private StageSelectionWidget _stageSelectionWidget;
        [SerializeField] private MutationSelectionWidget _mutationSelectionWidget;

        private IWallet _wallet;
        private IStageMapService _stageMapService;
        private IGameStateMachine _stateMachine;
        private UIManager _uiManager;
        private IEventBus _eventBus;
        private IAnalyticsService _analytics;
        private IAdService _adService;
        private IAdPolicy _adPolicy;
        private IRunConfigBuilder _runConfigBuilder;
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
        public Button BoostSquadButton => _boostSquadButton;
        public TMP_Text BoostSquadStatusText => _boostSquadStatusText;
        public bool IsPreRunBoostActive { get; private set; }
        public bool CanRequestPreRunBoost => !IsPreRunBoostActive && (_adPolicy == null || _adPolicy.CanShowRewarded(AdPlacement.PreRunBoost)) && (_adService == null || _adService.IsAdAvailable(AdPlacement.PreRunBoost));
        public StageSelectionWidget StageSelectionWidget => _stageSelectionWidget;
        public MutationSelectionWidget MutationSelectionWidget => _mutationSelectionWidget;
        public IReadOnlyList<string> SelectedMutations => _mutationSelectionWidget != null ? _mutationSelectionWidget.SelectedMutationIds : Array.Empty<string>();
        public TMP_Text NavigationFeedbackText => _navigationFeedbackText;
        public string LastNavigationShortcut { get; private set; }
        public Action DebutJourneyHook { get; set; }
        public Action BarracksHook { get; set; }

        public event Action<string> OnRunStarted;
        public event Action OnOptionsRequested;
        public event Action OnDebutJourneyRequested;
        public event Action OnBarracksRequested;
        public event Action<string> OnNavigationShortcutClicked;

        public string SelectedStageId => _stageSelectionWidget != null
            ? _stageSelectionWidget.SelectedStageId
            : (!string.IsNullOrEmpty(_fallbackSelectedStageId) ? _fallbackSelectedStageId : _stageMapService?.GetHighestUnlockedStage() ?? "stage_01");

        protected override void Awake()
        {
            SetLayerType(UILayerType.Hub);
            base.Awake();
            if (_stageSelectionWidget == null) _stageSelectionWidget = GetComponentInChildren<StageSelectionWidget>(true);
            if (_mutationSelectionWidget == null) _mutationSelectionWidget = GetComponentInChildren<MutationSelectionWidget>(true);
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
            OnBarracksRequested = null;
            OnNavigationShortcutClicked = null;
            DebutJourneyHook = null;
            BarracksHook = null;
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshCoins();
            _stageSelectionWidget?.Refresh();
            _mutationSelectionWidget?.Refresh();
        }

        public void BindComponents(TMP_Text coins = null, Button play = null, Button options = null, Button debut = null, Button barracks = null, Button shop = null, Button pass = null, StageSelectionWidget stageWidget = null, TMP_Text feedback = null, MutationSelectionWidget mutationWidget = null, Button boostSquad = null, TMP_Text boostSquadText = null)
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
            if (mutationWidget != null) _mutationSelectionWidget = mutationWidget;
            if (boostSquad != null) _boostSquadButton = boostSquad;
            if (boostSquadText != null) _boostSquadStatusText = boostSquadText;
            WireButtons();
            RefreshCoins();
            RefreshBoostUI();
            _stageSelectionWidget?.Refresh();
            _mutationSelectionWidget?.Refresh();
        }

        public void Initialize(
            IWallet wallet,
            IStageMapService stageMapService,
            IGameStateMachine stateMachine = null,
            UIManager uiManager = null,
            IEventBus eventBus = null,
            IAnalyticsService analytics = null,
            IMutationCatalog mutationCatalog = null,
            IProgressionService progressionService = null,
            IAdService adService = null,
            IAdPolicy adPolicy = null,
            IRunConfigBuilder runConfigBuilder = null)
        {
            _walletSubscription?.Dispose();
            _wallet = wallet;
            _stageMapService = stageMapService;
            _stateMachine = stateMachine;
            _uiManager = uiManager;
            _eventBus = eventBus;
            _analytics = analytics;
            _adService = adService;
            _adPolicy = adPolicy;
            _runConfigBuilder = runConfigBuilder;

            if (_eventBus != null) _walletSubscription = _eventBus.Subscribe<WalletChangedEvent>(OnWalletChanged);
            if (_stageSelectionWidget != null && stageMapService != null)
            {
                _stageSelectionWidget.Initialize(stageMapService);
                _stageSelectionWidget.OnBoostSquadClicked -= RequestPreRunBoostWithAd;
                _stageSelectionWidget.OnBoostSquadClicked += RequestPreRunBoostWithAd;
            }
            if (_mutationSelectionWidget != null && (mutationCatalog != null || progressionService != null)) _mutationSelectionWidget.Initialize(mutationCatalog, progressionService);

            WireButtons();
            RefreshCoins();
            RefreshBoostUI();
        }

        public bool SelectStage(string stageId)
        {
            if (_stageSelectionWidget != null) return _stageSelectionWidget.SelectStage(stageId);
            if (string.IsNullOrWhiteSpace(stageId) || (_stageMapService != null && !_stageMapService.IsStageUnlocked(stageId))) return false;
            _fallbackSelectedStageId = stageId;
            return true;
        }

        public void ActivatePreRunBoost()
        {
            IsPreRunBoostActive = true;
            RefreshBoostUI();
        }

        public void RequestPreRunBoostWithAd()
        {
            if (IsPreRunBoostActive) return;

            if (_adPolicy != null && !_adPolicy.CanShowRewarded(AdPlacement.PreRunBoost))
            {
                return;
            }

            if (_adService == null)
            {
                ActivatePreRunBoost();
                return;
            }

            if (!_adService.IsAdAvailable(AdPlacement.PreRunBoost))
            {
                return;
            }

            _adService.ShowAd(AdPlacement.PreRunBoost, result =>
            {
                if (result == AdShowResult.Completed)
                {
                    _adPolicy?.RecordAdShown(AdPlacement.PreRunBoost, AdType.Rewarded);
                    _analytics?.TrackEvent(AnalyticsEvents.AdReward, new Dictionary<string, object>
                    {
                        ["placement"] = "pre_run_boost"
                    });
                    ActivatePreRunBoost();
                }
                else
                {
                    _analytics?.TrackEvent(AnalyticsEvents.AdFailed, new Dictionary<string, object>
                    {
                        ["placement"] = "pre_run_boost",
                        ["reason"] = result.ToString()
                    });
                }
            });
        }

        public void RefreshBoostUI()
        {
            if (_boostSquadButton != null)
            {
                _boostSquadButton.interactable = !IsPreRunBoostActive && CanRequestPreRunBoost;
            }
            if (_boostSquadStatusText != null)
            {
                _boostSquadStatusText.text = IsPreRunBoostActive ? "+10 Tropa Ativo" : "+10 Tropa (Anúncio)";
            }
        }

        public bool StartSelectedRun()
        {
            string stageId = SelectedStageId;
            if (string.IsNullOrWhiteSpace(stageId) || (_stageMapService != null && !_stageMapService.IsStageUnlocked(stageId))) return false;

            bool ok = _stateMachine != null ? _stateMachine.TryTransition(GameState.Run) : (_uiManager != null && _uiManager.TryTransition(GameState.Run));
            if (ok)
            {
                RunConfig config;
                var activeBoosts = IsPreRunBoostActive ? new[] { "squad_plus_10" } : Array.Empty<string>();
                if (_runConfigBuilder != null)
                {
                    config = _runConfigBuilder.Build(stageId, SelectedMutations, activeBoosts);
                }
                else
                {
                    config = new RunConfig
                    {
                        LevelId = stageId,
                        MutationIds = new List<string>(SelectedMutations),
                        InitialSquad = IsPreRunBoostActive ? 20 : 10
                    };
                }
                IsPreRunBoostActive = false;
                RefreshBoostUI();
                _eventBus?.Publish(new RunStartedEvent(stageId, config));
                OnRunStarted?.Invoke(stageId);
                return true;
            }
            return false;
        }

        public void OpenOptions() { OnOptionsRequested?.Invoke(); _uiManager?.OpenWindow<OptionsView>(); }
        public void OpenDebutJourney() { OnDebutJourneyRequested?.Invoke(); DebutJourneyHook?.Invoke(); }

        public void OpenBarracks()
        {
            NavigateToShortcut("Quartel");
            OnBarracksRequested?.Invoke();
            BarracksHook?.Invoke();
            _uiManager?.OpenWindow<BarracksView>();
        }

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
            _barracksButton?.onClick.AddListener(OpenBarracks);
            _shopButton?.onClick.AddListener(() => NavigateToShortcut("Loja"));
            _passButton?.onClick.AddListener(() => NavigateToShortcut("Passe"));
            _boostSquadButton?.onClick.AddListener(RequestPreRunBoostWithAd);
        }

        private void UnwireButtons()
        {
            _playButton?.onClick.RemoveAllListeners();
            _optionsButton?.onClick.RemoveAllListeners();
            _debutJourneyButton?.onClick.RemoveAllListeners();
            _barracksButton?.onClick.RemoveAllListeners();
            _shopButton?.onClick.RemoveAllListeners();
            _passButton?.onClick.RemoveAllListeners();
            _boostSquadButton?.onClick.RemoveAllListeners();
        }
    }
}
