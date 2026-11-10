using Game.Core.Ads;

namespace Game.Meta.Ads
{
    public interface IAdPolicy
    {
        AdPolicyDefinition Definition { get; }
        AdPolicyState State { get; }
        bool CanShowInterstitial(int playerHighestStage);
        bool CanShowRewarded(AdPlacement placement);
        void RecordAdShown(AdPlacement placement, AdType adType);
        void RecordPurchase();
        void RecordSessionStart();
        void RecordStageCompleted();
    }
}
