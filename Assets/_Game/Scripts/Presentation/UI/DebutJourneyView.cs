using System;
using System.Collections.Generic;
using Game.Meta.Debut;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class DebutJourneyView : UIWindow
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _feedbackText;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text[] _dayStatusTexts;

        private DebutJourneyService _service;
        private IProgressTrack _track;
        private int _currentLoginDay;
        private IRewardSink _sink;

        public override UILayerType LayerType => UILayerType.Modal;

        public TMP_Text TitleText => _titleText;
        public TMP_Text FeedbackText => _feedbackText;
        public Button ClaimButton => _claimButton;
        public Button CloseButton => _closeButton;
        public IReadOnlyList<TMP_Text> DayStatusTexts => _dayStatusTexts;
        public DebutJourneyService Service => _service;
        public IProgressTrack Track => _track;
        public int CurrentLoginDay => _currentLoginDay;

        public event Action<int> OnDayClaimed;
        public event Action OnCloseClicked;

        protected override void Awake()
        {
            SetLayerType(UILayerType.Modal);
            base.Awake();
            WireButtons();
        }

        protected override void OnDestroy()
        {
            UnwireButtons();
            OnDayClaimed = null;
            OnCloseClicked = null;
            base.OnDestroy();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshView();
        }

        public void BindComponents(
            Button closeButton = null,
            Button claimButton = null,
            TMP_Text[] dayStatusTexts = null,
            TMP_Text titleText = null,
            TMP_Text feedbackText = null)
        {
            UnwireButtons();
            if (closeButton != null) _closeButton = closeButton;
            if (claimButton != null) _claimButton = claimButton;
            if (dayStatusTexts != null) _dayStatusTexts = dayStatusTexts;
            if (titleText != null) _titleText = titleText;
            if (feedbackText != null) _feedbackText = feedbackText;
            WireButtons();
            RefreshView();
        }

        public void Initialize(
            DebutJourneyService service,
            IProgressTrack track,
            int currentLoginDay,
            IRewardSink sink = null)
        {
            _service = service;
            _track = track;
            _currentLoginDay = currentLoginDay;
            _sink = sink;

            RefreshView();
        }

        public void SetCurrentLoginDay(int currentLoginDay)
        {
            _currentLoginDay = currentLoginDay;
            RefreshView();
        }

        public void RefreshView()
        {
            if (_service == null || _track == null)
            {
                if (_claimButton != null)
                {
                    _claimButton.interactable = false;
                }
                return;
            }

            var daysStatus = _service.GetDaysStatus(_track, _currentLoginDay);
            bool hasClaimableDay = false;

            if (_dayStatusTexts != null)
            {
                for (int i = 0; i < _dayStatusTexts.Length && i < daysStatus.Count; i++)
                {
                    if (_dayStatusTexts[i] != null)
                    {
                        var status = daysStatus[i];
                        _dayStatusTexts[i].text = FormatDayStatus(i + 1, status);
                    }
                }
            }

            for (int i = 0; i < daysStatus.Count; i++)
            {
                if (daysStatus[i] == DebutDayStatus.Available)
                {
                    hasClaimableDay = true;
                    break;
                }
            }

            if (_claimButton != null)
            {
                _claimButton.interactable = hasClaimableDay;
            }
        }

        public bool TryClaimDay(int dayIndex)
        {
            if (_service == null || _track == null || _sink == null)
            {
                return false;
            }

            bool claimed = _service.ClaimDay(_track, dayIndex, _currentLoginDay, _sink);
            if (claimed)
            {
                if (_feedbackText != null)
                {
                    _feedbackText.text = $"Dia {dayIndex + 1} resgatado!";
                }
                OnDayClaimed?.Invoke(dayIndex);
                RefreshView();
            }
            return claimed;
        }

        public bool ClaimAvailableDay()
        {
            if (_service == null || _track == null || _sink == null)
            {
                return false;
            }

            for (int i = 0; i < _service.Definition.Milestones.Count; i++)
            {
                if (_service.CanClaimDay(_track, i, _currentLoginDay))
                {
                    return TryClaimDay(i);
                }
            }

            return false;
        }

        private void HandleCloseClicked()
        {
            OnCloseClicked?.Invoke();
            Close();
        }

        private void WireButtons()
        {
            UnwireButtons();
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_claimButton != null)
            {
                _claimButton.onClick.AddListener(HandleClaimButtonClicked);
            }
        }

        private void UnwireButtons()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_claimButton != null)
            {
                _claimButton.onClick.RemoveListener(HandleClaimButtonClicked);
            }
        }

        private void HandleClaimButtonClicked()
        {
            ClaimAvailableDay();
        }

        private static string FormatDayStatus(int dayNumber, DebutDayStatus status)
        {
            return status switch
            {
                DebutDayStatus.Claimed => $"Dia {dayNumber}: Resgatado",
                DebutDayStatus.Available => $"Dia {dayNumber}: Disponivel",
                _ => $"Dia {dayNumber}: Bloqueado"
            };
        }
    }
}
