namespace Game.Meta.Wallet
{
    public readonly struct WalletChangedEvent
    {
        public CurrencyKind Currency { get; }
        public int NewBalance { get; }
        public int Delta { get; }
        public string Source { get; }
        public string TokenId { get; }

        public WalletChangedEvent(CurrencyKind currency, int newBalance, int delta, string source, string tokenId = null)
        {
            Currency = currency;
            NewBalance = newBalance;
            Delta = delta;
            Source = source;
            TokenId = tokenId;
        }
    }
}
