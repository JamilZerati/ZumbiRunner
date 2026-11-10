using System;

namespace Game.Core.Ads
{
    public interface IAdService
    {
        bool IsAdAvailable(AdPlacement placement);
        void ShowAd(AdPlacement placement, Action<AdShowResult> onComplete);
    }
}
