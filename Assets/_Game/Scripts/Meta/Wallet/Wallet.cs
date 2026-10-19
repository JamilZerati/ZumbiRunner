using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Meta.Wallet
{
    public class Wallet : IWallet
    {
        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, int> _balances = new();
        private readonly List<WalletTransaction> _history = new();

        public Wallet(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        private static string GetKey(CurrencyKind currency, string tokenId)
        {
            return currency == CurrencyKind.EventToken && !string.IsNullOrEmpty(tokenId)
                ? $"{currency}:{tokenId}"
                : currency.ToString();
        }

        public int GetBalance(CurrencyKind currency, string tokenId = null)
        {
            string key = GetKey(currency, tokenId);
            return _balances.TryGetValue(key, out int balance) ? balance : 0;
        }

        public void Add(CurrencyKind currency, int amount, string source, string tokenId = null)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source is required.", nameof(source));
            }

            string key = GetKey(currency, tokenId);
            int current = _balances.TryGetValue(key, out int val) ? val : 0;
            int newBalance = current + amount;
            _balances[key] = newBalance;

            var transaction = new WalletTransaction(currency, amount, source, tokenId, DateTime.UtcNow.Ticks);
            _history.Add(transaction);

            _eventBus?.Publish(new WalletChangedEvent(currency, newBalance, amount, source, tokenId));
        }

        public bool TrySpend(CurrencyKind currency, int amount, string source, string tokenId = null)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source is required.", nameof(source));
            }

            string key = GetKey(currency, tokenId);
            int current = _balances.TryGetValue(key, out int val) ? val : 0;
            if (current < amount)
            {
                return false;
            }

            int newBalance = current - amount;
            _balances[key] = newBalance;

            var transaction = new WalletTransaction(currency, -amount, source, tokenId, DateTime.UtcNow.Ticks);
            _history.Add(transaction);

            _eventBus?.Publish(new WalletChangedEvent(currency, newBalance, -amount, source, tokenId));
            return true;
        }

        public IReadOnlyList<WalletTransaction> GetHistory() => _history;
    }
}
