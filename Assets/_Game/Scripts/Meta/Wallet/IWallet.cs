using System.Collections.Generic;

namespace Game.Meta.Wallet
{
    public interface IWallet
    {
        int GetBalance(CurrencyKind currency, string tokenId = null);
        void Add(CurrencyKind currency, int amount, string source, string tokenId = null);
        bool TrySpend(CurrencyKind currency, int amount, string source, string tokenId = null);
        IReadOnlyList<WalletTransaction> GetHistory();
    }
}
