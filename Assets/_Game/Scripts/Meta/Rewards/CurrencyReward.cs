using System;
using Game.Meta.Wallet;

namespace Game.Meta.Rewards
{
    public class CurrencyReward : IReward
    {
        public string RewardType => "Currency";
        public CurrencyKind Currency { get; }
        public int Amount { get; }
        public string TokenId { get; }

        public CurrencyReward(CurrencyKind currency, int amount, string tokenId = null)
        {
            Currency = currency;
            Amount = amount;
            TokenId = tokenId;
        }

        public void Apply(IRewardSink sink) => throw new NotImplementedException();
    }
}
