using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class ResultView : UIWindow
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private TMP_Text _multiplierText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private Button _doubleRewardButton;
        [SerializeField] private Button _continueButton;

        private ResultRewardPresenter _presenter;

        public override UILayerType LayerType => UILayerType.Modal;

        public TMP_Text TitleText => _titleText;
        public TMP_Text CoinsText => _coinsText;
        public TMP_Text MultiplierText => _multiplierText;
        public TMP_Text StarsText => _starsText;
        public Button DoubleRewardButton => _doubleRewardButton;
        public Button ContinueButton => _continueButton;
        public ResultRewardPresenter Presenter => _presenter;

        protected override void Awake()
        {
            SetLayerType(UILayerType.Modal);
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnbindButtons();
        }

        public void BindComponents(
            TMP_Text titleText = null,
            TMP_Text coinsText = null,
            TMP_Text multiplierText = null,
            TMP_Text starsText = null,
            Button doubleRewardButton = null,
            Button continueButton = null)
        {
            if (titleText != null) _titleText = titleText;
            if (coinsText != null) _coinsText = coinsText;
            if (multiplierText != null) _multiplierText = multiplierText;
            if (starsText != null) _starsText = starsText;
            if (doubleRewardButton != null) _doubleRewardButton = doubleRewardButton;
            if (continueButton != null) _continueButton = continueButton;
        }

        public void Initialize(ResultRewardPresenter presenter)
        {
            _presenter = presenter;
            UnbindButtons();

            if (_doubleRewardButton != null) _doubleRewardButton.onClick.AddListener(OnDoubleRewardClicked);
            if (_continueButton != null) _continueButton.onClick.AddListener(OnContinueClicked);
            if (_presenter != null) RefreshView(_presenter);
        }

        public void RefreshView(ResultRewardPresenter presenter)
        {
            if (presenter == null) return;

            var result = presenter.CurrentResult;
            if (_titleText != null && result != null)
            {
                _titleText.text = result.Victory ? "Vitória" : "Derrota";
            }

            if (_starsText != null && result != null)
            {
                _starsText.text = result.Stars.ToString();
            }

            if (_coinsText != null)
            {
                _coinsText.text = presenter.TotalCoins.ToString();
            }

            if (_multiplierText != null)
            {
                float displayMultiplier = presenter.Multiplier * (presenter.IsDoubled ? 2.0f : 1.0f);
                _multiplierText.text = $"{displayMultiplier:0.#}x";
            }

            if (_doubleRewardButton != null)
            {
                _doubleRewardButton.interactable = !presenter.IsDoubled && !presenter.IsClaimed;
            }

            if (_continueButton != null)
            {
                _continueButton.interactable = !presenter.IsClaimed;
            }
        }

        public void SetButtonsInteractable(bool interactable)
        {
            if (_doubleRewardButton != null) _doubleRewardButton.interactable = interactable;
            if (_continueButton != null) _continueButton.interactable = interactable;
        }

        private void OnDoubleRewardClicked() => _presenter?.DoubleReward();
        private void OnContinueClicked() => _presenter?.ClaimAndContinue();

        private void UnbindButtons()
        {
            if (_doubleRewardButton != null) _doubleRewardButton.onClick.RemoveListener(OnDoubleRewardClicked);
            if (_continueButton != null) _continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}
