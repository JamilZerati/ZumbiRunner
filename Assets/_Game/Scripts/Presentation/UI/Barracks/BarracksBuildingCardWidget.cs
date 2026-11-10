using System;
using Game.Meta.Barracks;
using Game.Meta.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Barracks
{
    public class BarracksBuildingCardWidget : MonoBehaviour
    {
        [SerializeField] private BarracksBuildingType _type;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _coinCostText;
        [SerializeField] private TMP_Text _recruitCostText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private Button _speedupAdButton;
        [SerializeField] private Button _instantMedalButton;
        [SerializeField] private TMP_Text _instantMedalCostText;

        private IBarracksService _service;
        private BarracksBuildingDefinition _definition;
        private IWallet _wallet;

        public BarracksBuildingType BuildingType => _type;
        public TMP_Text TitleText => _titleText;
        public TMP_Text LevelText => _levelText;
        public TMP_Text CoinCostText => _coinCostText;
        public TMP_Text RecruitCostText => _recruitCostText;
        public TMP_Text TimerText => _timerText;
        public Button UpgradeButton => _upgradeButton;
        public Button SpeedupAdButton => _speedupAdButton;
        public Button InstantMedalButton => _instantMedalButton;
        public TMP_Text InstantMedalCostText => _instantMedalCostText;

        public event Action<BarracksBuildingType> OnUpgradeClicked;
        public event Action<BarracksBuildingType> OnSpeedupAdClicked;
        public event Action<BarracksBuildingType> OnInstantMedalClicked;

        private void Awake()
        {
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
            OnUpgradeClicked = null;
            OnSpeedupAdClicked = null;
            OnInstantMedalClicked = null;
        }

        public void BindComponents(
            TMP_Text titleText = null,
            TMP_Text levelText = null,
            TMP_Text coinCostText = null,
            TMP_Text recruitCostText = null,
            TMP_Text timerText = null,
            Button upgradeButton = null,
            Button speedupAdButton = null,
            Button instantMedalButton = null,
            TMP_Text instantMedalCostText = null,
            BarracksBuildingType type = BarracksBuildingType.Recruitment)
        {
            UnwireButtons();
            _type = type;
            if (titleText != null) _titleText = titleText;
            if (levelText != null) _levelText = levelText;
            if (coinCostText != null) _coinCostText = coinCostText;
            if (recruitCostText != null) _recruitCostText = recruitCostText;
            if (timerText != null) _timerText = timerText;
            if (upgradeButton != null) _upgradeButton = upgradeButton;
            if (speedupAdButton != null) _speedupAdButton = speedupAdButton;
            if (instantMedalButton != null) _instantMedalButton = instantMedalButton;
            if (instantMedalCostText != null) _instantMedalCostText = instantMedalCostText;
            WireButtons();
        }

        public void Initialize(
            BarracksBuildingType type,
            IBarracksService service,
            BarracksBuildingDefinition definition,
            IWallet wallet = null)
        {
            _type = type;
            _service = service;
            _definition = definition;
            _wallet = wallet;

            Refresh(_wallet);
        }

        public void Refresh(IWallet wallet = null)
        {
            if (wallet != null) _wallet = wallet;
            if (_service == null) return;

            int currentLevel = _service.GetBuildingLevel(_type);
            int maxLevel = _definition?.MaxLevel ?? 5;
            bool isMax = currentLevel >= maxLevel;
            bool isConstructing = _service.IsUnderConstruction(_type);

            if (_titleText != null && _definition != null)
            {
                _titleText.text = _definition.DisplayName;
            }

            if (_levelText != null)
            {
                _levelText.text = isMax ? $"Nível {currentLevel} (Máx)" : $"Nível {currentLevel}";
            }

            if (isMax)
            {
                if (_coinCostText != null) _coinCostText.text = "-";
                if (_recruitCostText != null) _recruitCostText.text = "-";
                if (_timerText != null) _timerText.text = "Nível Máximo";
                if (_upgradeButton != null) _upgradeButton.interactable = false;
                if (_speedupAdButton != null) _speedupAdButton.gameObject.SetActive(false);
                if (_instantMedalButton != null) _instantMedalButton.gameObject.SetActive(false);
                return;
            }

            if (isConstructing)
            {
                TimeSpan remaining = _service.GetRemainingConstructionTime(_type);
                if (remaining <= TimeSpan.Zero)
                {
                    if (_timerText != null) _timerText.text = "Concluído";
                    if (_upgradeButton != null)
                    {
                        _upgradeButton.interactable = true;
                        var btnTxt = _upgradeButton.GetComponentInChildren<TMP_Text>();
                        if (btnTxt != null) btnTxt.text = "Concluir";
                    }
                    if (_speedupAdButton != null) _speedupAdButton.gameObject.SetActive(false);
                    if (_instantMedalButton != null) _instantMedalButton.gameObject.SetActive(false);
                }
                else
                {
                    if (_timerText != null)
                    {
                        _timerText.text = $"{remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                    }
                    if (_upgradeButton != null) _upgradeButton.interactable = false;
                    if (_speedupAdButton != null)
                    {
                        _speedupAdButton.gameObject.SetActive(true);
                        _speedupAdButton.interactable = !_service.IsClockManipulated;
                    }
                    if (_instantMedalButton != null)
                    {
                        _instantMedalButton.gameObject.SetActive(true);
                        int medalCost = _service.GetInstantCompleteMedalCost(_type);
                        if (_instantMedalCostText != null)
                        {
                            _instantMedalCostText.text = $"{medalCost} Medalhas";
                        }
                        int medalBalance = _wallet?.GetBalance(CurrencyKind.Medals) ?? 0;
                        _instantMedalButton.interactable = medalBalance >= medalCost && !_service.IsClockManipulated;
                    }
                }

                if (_coinCostText != null) _coinCostText.text = "-";
                if (_recruitCostText != null) _recruitCostText.text = "-";
            }
            else
            {
                int coinCost = _service.GetUpgradeCoinCost(_type);
                int recruitCost = _service.GetUpgradeRecruitCost(_type);

                if (_coinCostText != null) _coinCostText.text = coinCost.ToString();
                if (_recruitCostText != null) _recruitCostText.text = recruitCost.ToString();
                if (_timerText != null) _timerText.text = "";

                bool canUpgrade = _service.CanStartUpgrade(_type, out _);
                if (_upgradeButton != null)
                {
                    _upgradeButton.interactable = canUpgrade;
                    var btnTxt = _upgradeButton.GetComponentInChildren<TMP_Text>();
                    if (btnTxt != null) btnTxt.text = "Evoluir";
                }
                if (_speedupAdButton != null) _speedupAdButton.gameObject.SetActive(false);
                if (_instantMedalButton != null) _instantMedalButton.gameObject.SetActive(false);
            }
        }

        private void WireButtons()
        {
            UnwireButtons();
            _upgradeButton?.onClick.AddListener(HandleUpgradeClicked);
            _speedupAdButton?.onClick.AddListener(HandleSpeedupAdClicked);
            _instantMedalButton?.onClick.AddListener(HandleInstantMedalClicked);
        }

        private void UnwireButtons()
        {
            _upgradeButton?.onClick.RemoveAllListeners();
            _speedupAdButton?.onClick.RemoveAllListeners();
            _instantMedalButton?.onClick.RemoveAllListeners();
        }

        private void HandleUpgradeClicked()
        {
            if (_service == null) return;
            if (_service.IsUnderConstruction(_type))
            {
                if (_service.GetRemainingConstructionTime(_type) <= TimeSpan.Zero)
                {
                    _service.TryCompleteUpgrade(_type);
                }
            }
            else
            {
                _service.TryStartUpgrade(_type);
            }
            OnUpgradeClicked?.Invoke(_type);
            Refresh(_wallet);
        }

        private void HandleSpeedupAdClicked()
        {
            if (_service == null) return;
            _service.TrySpeedupWithAd(_type);
            OnSpeedupAdClicked?.Invoke(_type);
            Refresh(_wallet);
        }

        private void HandleInstantMedalClicked()
        {
            if (_service == null) return;
            _service.TryInstantCompleteWithMedals(_type);
            OnInstantMedalClicked?.Invoke(_type);
            Refresh(_wallet);
        }
    }
}
