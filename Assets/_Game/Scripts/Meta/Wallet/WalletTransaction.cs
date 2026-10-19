namespace Game.Meta.Wallet
{
    public readonly struct WalletTransaction
    {
        public CurrencyKind Currency { get; }
        public int Amount { get; }
        public string Source { get; }
        public string TokenId { get; }
        public long TimestampTicks { get; }

        public WalletTransaction(CurrencyKind currency, int amount, string source, string tokenId = null, long timestampTicks = 0)
        {
            Currency = currency;
            Amount = amount;
            Source = source;
            TokenId = tokenId;
            TimestampTicks = timestampTicks;
        }
    }
}
