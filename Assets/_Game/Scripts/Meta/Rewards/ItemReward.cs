using System;

namespace Game.Meta.Rewards
{
    public class ItemReward : IReward
    {
        public string RewardType => "Item";
        public string ItemId { get; }
        public int Quantity { get; }

        public ItemReward(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public void Apply(IRewardSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddItem(ItemId, Quantity);
        }
    }
}
