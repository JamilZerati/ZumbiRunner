using System;
using System.Collections.Generic;
using Game.Core.Analytics;

namespace Game.Core.Ads
{
    public class MockAdService : IAdService
    {
        private readonly IAnalyticsService _analytics;
        private readonly List<AdCallRecord> _history = new();
        private readonly Dictionary<AdPlacement, AdShowResult> _placementResults = new();
        private readonly Dictionary<AdPlacement, bool> _placementAvailability = new();

        public AdShowResult DefaultResult { get; set; } = AdShowResult.Completed;
        public bool DefaultAvailability { get; set; } = true;
        public IReadOnlyList<AdCallRecord> History => _history;
        public int ShowAdCallCount => _history.Count;

        public MockAdService(IAnalyticsService analytics = null)
        {
            _analytics = analytics;
        }

        public void SetPlacementResult(AdPlacement placement, AdShowResult result)
        {
            _placementResults[placement] = result;
        }

        public void SetPlacementAvailable(AdPlacement placement, bool isAvailable)
        {
            _placementAvailability[placement] = isAvailable;
        }

        public bool IsAdAvailable(AdPlacement placement)
        {
            return _placementAvailability.TryGetValue(placement, out var available)
                ? available
                : DefaultAvailability;
        }

        public void ShowAd(AdPlacement placement, Action<AdShowResult> onComplete)
        {
            var adType = placement == AdPlacement.InterLevel ? AdType.Interstitial : AdType.Rewarded;

            var result = !IsAdAvailable(placement)
                ? AdShowResult.NotAvailable
                : (_placementResults.TryGetValue(placement, out var configuredResult) ? configuredResult : DefaultResult);

            if (_analytics != null)
            {
                var shownParams = new Dictionary<string, object>
                {
                    ["placement"] = placement.ToString(),
                    ["ad_type"] = adType.ToString()
                };
                _analytics.TrackEvent(AnalyticsEvents.AdShown, shownParams);

                if (result == AdShowResult.Completed)
                {
                    var rewardParams = new Dictionary<string, object>
                    {
                        ["placement"] = placement.ToString()
                    };
                    _analytics.TrackEvent(AnalyticsEvents.AdReward, rewardParams);
                }
                else
                {
                    var failedParams = new Dictionary<string, object>
                    {
                        ["placement"] = placement.ToString(),
                        ["reason"] = result.ToString()
                    };
                    _analytics.TrackEvent(AnalyticsEvents.AdFailed, failedParams);
                }
            }

            _history.Add(new AdCallRecord(placement, adType, result));
            onComplete?.Invoke(result);
        }

        public bool HasShown(AdPlacement placement)
        {
            for (var i = 0; i < _history.Count; i++)
            {
                if (_history[i].Placement == placement)
                {
                    return true;
                }
            }

            return false;
        }

        public void Reset()
        {
            _history.Clear();
            _placementResults.Clear();
            _placementAvailability.Clear();
            DefaultResult = AdShowResult.Completed;
            DefaultAvailability = true;
        }
    }

    public readonly struct AdCallRecord
    {
        public AdPlacement Placement { get; }
        public AdType Type { get; }
        public AdShowResult Result { get; }

        public AdCallRecord(AdPlacement placement, AdType type, AdShowResult result)
        {
            Placement = placement;
            Type = type;
            Result = result;
        }
    }
}
