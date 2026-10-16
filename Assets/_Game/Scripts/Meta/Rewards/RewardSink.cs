using System.Collections.Generic;
using Game.Meta.Tracks;
using Game.Meta.Wallet;

namespace Game.Meta.Rewards
{
    public class RewardSink : IRewardSink
    {
        private readonly HashSet<string> _grantedIds = new();
        private readonly Dictionary<string, int> _items = new();
        private readonly HashSet<string> _unlockedContent = new();

        public IWallet Wallet { get; }
        public IEntitlements Entitlements { get; }

        public RewardSink(IWallet wallet = null, IEntitlements entitlements = null)
        {
            Wallet = wallet;
            Entitlements = entitlements;
        }

        public bool HasGranted(string grantId)
        {
            return !string.IsNullOrEmpty(grantId) && _grantedIds.Contains(grantId);
        }

        public void RecordGrant(string grantId)
        {
            if (!string.IsNullOrEmpty(grantId))
            {
                _grantedIds.Add(grantId);
            }
        }

        public void AddItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;
            _items[itemId] = (_items.TryGetValue(itemId, out int current) ? current : 0) + quantity;
        }

        public void UnlockContent(string contentId)
        {
            if (!string.IsNullOrEmpty(contentId))
            {
                _unlockedContent.Add(contentId);
            }
        }

        public int GetItemQuantity(string itemId) => _items.TryGetValue(itemId, out int q) ? q : 0;
        public bool IsContentUnlocked(string contentId) => _unlockedContent.Contains(contentId);
    }
}
