using System;
using Game.Core.Ads;
using Game.Core.Analytics;

namespace Game.Infrastructure.Ads
{
    public class LevelPlayAdService : IAdService
    {
        private readonly IAdService _fallbackService;
        private readonly IAnalyticsService _analytics;
        private string _appKey;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public bool IsUsingFallback { get; private set; }
        public string AppKey => _appKey;
        public IAdService FallbackService => _fallbackService;

        public LevelPlayAdService(IAdService fallbackService = null, string appKey = null, IAnalyticsService analytics = null)
        {
            _analytics = analytics;
            _fallbackService = fallbackService ?? new MockAdService(analytics);
            _appKey = appKey;
            IsUsingFallback = true;
            if (!string.IsNullOrEmpty(appKey))
            {
                Initialize(appKey);
            }
        }

        public LevelPlayAdService(IAnalyticsService analytics)
            : this(null, null, analytics)
        {
        }

        public void Initialize(string appKey)
        {
            _appKey = appKey;
            _isInitialized = true;
            IsUsingFallback = true;
        }

        public bool IsAdAvailable(AdPlacement placement)
        {
            return _fallbackService != null && _fallbackService.IsAdAvailable(placement);
        }

        public void ShowAd(AdPlacement placement, Action<AdShowResult> onComplete)
        {
            if (_fallbackService != null)
            {
                _fallbackService.ShowAd(placement, onComplete);
            }
            else
            {
                onComplete?.Invoke(AdShowResult.NotAvailable);
            }
        }
    }
}
