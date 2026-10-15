using System;
using System.Collections.Generic;

namespace Game.Meta.Wallet
{
    public class Wallet : IWallet
    {
        public int GetBalance(CurrencyKind currency, string tokenId = null) => throw new NotImplementedException();
        public void Add(CurrencyKind currency, int amount, string source, string tokenId = null) => throw new NotImplementedException();
        public bool TrySpend(CurrencyKind currency, int amount, string source, string tokenId = null) => throw new NotImplementedException();
        public IReadOnlyList<WalletTransaction> GetHistory() => throw new NotImplementedException();
    }
}
