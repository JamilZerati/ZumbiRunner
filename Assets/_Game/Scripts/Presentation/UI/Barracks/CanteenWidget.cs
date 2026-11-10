using System;
using Game.Meta.Barracks;
using Game.Meta.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Barracks
{
    public class CanteenWidget : MonoBehaviour
    {
        [SerializeField] private TMP_Text _pendingCoinsText;
        [SerializeField] private Button _collectButton;
        [SerializeField] private Button _doubleAdButton;
        [SerializeField] private TMP_Text _clockWarningText;
        [SerializeField] private GameObject _clockWarningObject;

        private IBarracksService _service;
        private IWallet _wallet;

        public TMP_Text PendingCoinsText => _pendingCoinsText;
        public Button CollectButton => _collectButton;
        public Button DoubleAdButton => _doubleAdButton;
        public TMP_Text ClockWarningText => _clockWarningText;
        public GameObject ClockWarningObject => _clockWarningObject;

        public event Action<bool, int> OnCollectClicked;

        private void Awake()
        {
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
            OnCollectClicked = null;
        }

        public void BindComponents(
            TMP_Text pendingCoinsText = null,
            Button collectButton = null,
            Button doubleAdButton = null,
            TMP_Text clockWarningText = null,
            GameObject clockWarningObject = null)
        {
            UnwireButtons();
            if (pendingCoinsText != null) _pendingCoinsText = pendingCoinsText;
            if (collectButton != null) _collectButton = collectButton;
            if (doubleAdButton != null) _doubleAdButton = doubleAdButton;
            if (clockWarningText != null) _clockWarningText = clockWarningText;
            if (clockWarningObject != null) _clockWarningObject = clockWarningObject;
            WireButtons();
        }

        public void Initialize(IBarracksService service, IWallet wallet = null)
        {
            _service = service;
            _wallet = wallet;
            Refresh();
        }

        public void Refresh()
        {
            if (_service == null)
            {
                if (_collectButton != null) _collectButton.interactable = false;
                if (_doubleAdButton != null) _doubleAdButton.interactable = false;
                return;
            }

            int pending = _service.GetPendingOfflineCoins();
            bool isClockManipulated = _service.IsClockManipulated;

            if (_pendingCoinsText != null)
            {
                _pendingCoinsText.text = pending.ToString();
            }

            if (_clockWarningText != null)
            {
                _clockWarningText.gameObject.SetActive(isClockManipulated);
            }

            if (_clockWarningObject != null)
            {
                _clockWarningObject.SetActive(isClockManipulated);
            }

            bool canCollect = pending > 0 && !isClockManipulated;
            if (_collectButton != null)
            {
                _collectButton.interactable = canCollect;
            }

            if (_doubleAdButton != null)
            {
                _doubleAdButton.interactable = canCollect;
            }
        }

        private void WireButtons()
        {
            UnwireButtons();
            _collectButton?.onClick.AddListener(HandleCollectStandard);
            _doubleAdButton?.onClick.AddListener(HandleCollectDouble);
        }

        private void UnwireButtons()
        {
            _collectButton?.onClick.RemoveAllListeners();
            _doubleAdButton?.onClick.RemoveAllListeners();
        }

        private void HandleCollectStandard()
        {
            if (_service == null) return;
            if (_service.TryCollectOfflineCoins(false, out int coins))
            {
                OnCollectClicked?.Invoke(false, coins);
            }
            Refresh();
        }

        private void HandleCollectDouble()
        {
            if (_service == null) return;
            if (_service.TryCollectOfflineCoins(true, out int coins))
            {
                OnCollectClicked?.Invoke(true, coins);
            }
            Refresh();
        }
    }
}
