using Game.Meta.Tracks;
using Game.Meta.Wallet;

namespace Game.Meta.Rewards
{
    public interface IRewardSink
    {
        IWallet Wallet { get; }
        IEntitlements Entitlements { get; }
        bool HasGranted(string grantId);
        void RecordGrant(string grantId);
        void AddItem(string itemId, int quantity);
        void UnlockContent(string contentId);
    }
}
