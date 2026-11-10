using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Meta.Barracks;
using Game.Meta.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Barracks
{
    public class BarracksView : UIWindow
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private CanteenWidget _canteenWidget;
        [SerializeField] private List<BarracksBuildingCardWidget> _buildingCards = new();
        [SerializeField] private TMP_Text _clockWarningText;
        [SerializeField] private GameObject _clockWarningObject;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _recruitsText;
        [SerializeField] private TMP_Text _medalsText;

        private IBarracksService _service;
        private IBarracksCatalog _catalog;
        private IWallet _wallet;
        private IEventBus _eventBus;
        private IDisposable _walletSubscription;
        private float _timerAccumulator;

        public override UILayerType LayerType => UILayerType.Modal;

        public Button CloseButton => _closeButton;
        public CanteenWidget CanteenWidget => _canteenWidget;
        public IReadOnlyList<BarracksBuildingCardWidget> BuildingCards => _buildingCards;
        public TMP_Text ClockWarningText => _clockWarningText;
        public GameObject ClockWarningObject => _clockWarningObject;
        public TMP_Text CoinsText => _coinsText;
        public TMP_Text RecruitsText => _recruitsText;
        public TMP_Text MedalsText => _medalsText;
        public IBarracksService Service => _service;
        public IBarracksCatalog Catalog => _catalog;
        public IWallet Wallet => _wallet;

        public event Action OnCloseClicked;

        protected override void Awake()
        {
            SetLayerType(UILayerType.Modal);
            base.Awake();
            WireButtons();
        }

        protected override void OnDestroy()
        {
            _walletSubscription?.Dispose();
            _walletSubscription = null;
            UnwireButtons();
            OnCloseClicked = null;
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsOpen) return;

            _timerAccumulator += Time.unscaledDeltaTime;
            if (_timerAccumulator >= 1.0f)
            {
                _timerAccumulator = 0f;
                RefreshTimers();
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshView();
        }

        public void OpenWindow()
        {
            Open();
        }

        public void CloseWindow()
        {
            Close();
        }

        public void BindComponents(
            Button closeButton = null,
            CanteenWidget canteenWidget = null,
            List<BarracksBuildingCardWidget> buildingCards = null,
            TMP_Text clockWarningText = null,
            GameObject clockWarningObject = null,
            TMP_Text coinsText = null,
            TMP_Text recruitsText = null,
            TMP_Text medalsText = null)
        {
            UnwireButtons();
            if (closeButton != null) _closeButton = closeButton;
            if (canteenWidget != null) _canteenWidget = canteenWidget;
            if (buildingCards != null) _buildingCards = buildingCards;
            if (clockWarningText != null) _clockWarningText = clockWarningText;
            if (clockWarningObject != null) _clockWarningObject = clockWarningObject;
            if (coinsText != null) _coinsText = coinsText;
            if (recruitsText != null) _recruitsText = recruitsText;
            if (medalsText != null) _medalsText = medalsText;
            WireButtons();
            RefreshView();
        }

        public void Initialize(
            IBarracksService service,
            IBarracksCatalog catalog,
            IWallet wallet = null,
            IEventBus eventBus = null)
        {
            _walletSubscription?.Dispose();
            _service = service;
            _catalog = catalog;
            _wallet = wallet;
            _eventBus = eventBus;

            if (_eventBus != null)
            {
                _walletSubscription = _eventBus.Subscribe<WalletChangedEvent>(OnWalletChanged);
            }

            if (_canteenWidget != null && _service != null)
            {
                _canteenWidget.Initialize(_service, _wallet);
            }

            if (_buildingCards != null && _service != null && _catalog != null)
            {
                for (int i = 0; i < _buildingCards.Count; i++)
                {
                    var card = _buildingCards[i];
                    if (card != null)
                    {
                        var def = _catalog.Get(card.BuildingType);
                        card.Initialize(card.BuildingType, _service, def, _wallet);
                    }
                }
            }

            RefreshView();
        }

        public BarracksBuildingCardWidget GetCard(BarracksBuildingType type)
        {
            if (_buildingCards == null) return null;
            for (int i = 0; i < _buildingCards.Count; i++)
            {
                if (_buildingCards[i] != null && _buildingCards[i].BuildingType == type)
                {
                    return _buildingCards[i];
                }
            }
            return null;
        }

        public void RefreshView()
        {
            RefreshBalances();
            RefreshClockWarning();
            _canteenWidget?.Refresh();

            if (_buildingCards != null)
            {
                for (int i = 0; i < _buildingCards.Count; i++)
                {
                    _buildingCards[i]?.Refresh(_wallet);
                }
            }
        }

        public void RefreshTimers()
        {
            RefreshClockWarning();
            _canteenWidget?.Refresh();

            if (_buildingCards != null)
            {
                for (int i = 0; i < _buildingCards.Count; i++)
                {
                    _buildingCards[i]?.Refresh(_wallet);
                }
            }
        }

        private void RefreshBalances()
        {
            if (_wallet == null) return;
            if (_coinsText != null) _coinsText.text = _wallet.GetBalance(CurrencyKind.Coins).ToString();
            if (_recruitsText != null) _recruitsText.text = _wallet.GetBalance(CurrencyKind.Recruits).ToString();
            if (_medalsText != null) _medalsText.text = _wallet.GetBalance(CurrencyKind.Medals).ToString();
        }

        private void RefreshClockWarning()
        {
            bool isClockManipulated = _service?.IsClockManipulated ?? false;
            if (_clockWarningText != null) _clockWarningText.gameObject.SetActive(isClockManipulated);
            if (_clockWarningObject != null) _clockWarningObject.SetActive(isClockManipulated);
        }

        private void OnWalletChanged(WalletChangedEvent evt)
        {
            RefreshView();
        }

        private void WireButtons()
        {
            UnwireButtons();
            _closeButton?.onClick.AddListener(HandleCloseClicked);
        }

        private void UnwireButtons()
        {
            _closeButton?.onClick.RemoveAllListeners();
        }

        private void HandleCloseClicked()
        {
            OnCloseClicked?.Invoke();
            CloseWindow();
        }
    }
}
