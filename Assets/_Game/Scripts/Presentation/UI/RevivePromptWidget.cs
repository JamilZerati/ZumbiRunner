using System;
using System.Collections.Generic;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Gameplay;
using Game.Meta.Ads;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class RevivePromptWidget : MonoBehaviour
    {
        [SerializeField] private Button _watchAdButton;
        [SerializeField] private Button _declineButton;
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private TMP_Text _timerText;

        private LevelDirector _director;
        private IAdService _adService;
        private IAdPolicy _adPolicy;
        private IAnalyticsService _analytics;
        private bool _isShowingAd;

        public Button WatchAdButton => _watchAdButton;
        public Button DeclineButton => _declineButton;
        public TMP_Text PromptText => _promptText;
        public TMP_Text TimerText => _timerText;
        public bool IsShowingAd => _isShowingAd;

        private void Awake()
        {
            WireButtons();
        }

        private void OnDestroy()
        {
            UnwireButtons();
            if (_director != null)
            {
                _director.OnReviveOffered -= HandleReviveOffered;
            }
        }

        public void BindComponents(Button watchAdButton = null, Button declineButton = null, TMP_Text promptText = null, TMP_Text timerText = null)
        {
            UnwireButtons();
            if (watchAdButton != null) _watchAdButton = watchAdButton;
            if (declineButton != null) _declineButton = declineButton;
            if (promptText != null) _promptText = promptText;
            if (timerText != null) _timerText = timerText;
            WireButtons();
        }

        public void Initialize(LevelDirector director, IAdService adService = null, IAdPolicy adPolicy = null, IAnalyticsService analytics = null)
        {
            if (_director != null)
            {
                _director.OnReviveOffered -= HandleReviveOffered;
            }

            _director = director;
            _adService = adService;
            _adPolicy = adPolicy;
            _analytics = analytics;

            if (_director != null)
            {
                _director.OnReviveOffered += HandleReviveOffered;
            }

            WireButtons();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            _analytics?.TrackEvent(AnalyticsEvents.AdOffer, new Dictionary<string, object>
            {
                ["placement"] = "revive"
            });
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Refresh()
        {
            bool canShowAd = (_adPolicy == null || _adPolicy.CanShowRewarded(AdPlacement.Revive)) &&
                             (_adService == null || _adService.IsAdAvailable(AdPlacement.Revive)) &&
                             !_isShowingAd;

            if (_watchAdButton != null)
            {
                _watchAdButton.interactable = canShowAd;
            }
        }

        public void HandleWatchAdClicked()
        {
            if (_isShowingAd || _director == null)
            {
                return;
            }

            if (_adPolicy != null && !_adPolicy.CanShowRewarded(AdPlacement.Revive))
            {
                HandleDeclineClicked();
                return;
            }

            if (_adService == null)
            {
                _director.ReviveSquad(10);
                Hide();
                return;
            }

            if (!_adService.IsAdAvailable(AdPlacement.Revive))
            {
                HandleDeclineClicked();
                return;
            }

            _isShowingAd = true;
            if (_watchAdButton != null) _watchAdButton.interactable = false;
            if (_declineButton != null) _declineButton.interactable = false;

            _adService.ShowAd(AdPlacement.Revive, result =>
            {
                _isShowingAd = false;
                if (result == AdShowResult.Completed)
                {
                    _adPolicy?.RecordAdShown(AdPlacement.Revive, AdType.Rewarded);
                    _analytics?.TrackEvent(AnalyticsEvents.AdReward, new Dictionary<string, object>
                    {
                        ["placement"] = "revive"
                    });
                    _director.ReviveSquad(10);
                    Hide();
                }
                else
                {
                    _analytics?.TrackEvent(AnalyticsEvents.AdFailed, new Dictionary<string, object>
                    {
                        ["placement"] = "revive",
                        ["reason"] = result.ToString()
                    });
                    _director.EndRun(victory: false);
                    Hide();
                }
            });
        }

        public void HandleDeclineClicked()
        {
            if (_isShowingAd)
            {
                return;
            }

            _director?.EndRun(victory: false);
            Hide();
        }

        private void HandleReviveOffered()
        {
            Show();
        }

        private void WireButtons()
        {
            UnwireButtons();
            _watchAdButton?.onClick.AddListener(HandleWatchAdClicked);
            _declineButton?.onClick.AddListener(HandleDeclineClicked);
        }

        private void UnwireButtons()
        {
            _watchAdButton?.onClick.RemoveAllListeners();
            _declineButton?.onClick.RemoveAllListeners();
        }
    }
}
