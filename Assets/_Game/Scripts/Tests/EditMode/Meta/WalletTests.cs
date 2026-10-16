using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class WalletTests
    {
        private EventBus _eventBus;
        private List<WalletChangedEvent> _events;
        private Wallet _wallet;

        [SetUp]
        public void SetUp()
        {
            _eventBus = new EventBus();
            _events = new List<WalletChangedEvent>();
            _eventBus.Subscribe<WalletChangedEvent>(e => _events.Add(e));
            _wallet = new Wallet(_eventBus);
        }

        [Test]
        public void InitialBalance_IsZero_ForAnyCurrency()
        {
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.Medals));
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.EventToken, "halloween"));
        }

        [Test]
        public void Add_IncreasesBalance_EmitsEvent_AndRecordsHistory()
        {
            _wallet.Add(CurrencyKind.Coins, 250, "battle_pass");

            Assert.AreEqual(250, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(CurrencyKind.Coins, _events[0].Currency);
            Assert.AreEqual(250, _events[0].NewBalance);
            Assert.AreEqual(250, _events[0].Delta);
            Assert.AreEqual("battle_pass", _events[0].Source);

            IReadOnlyList<WalletTransaction> history = _wallet.GetHistory();
            Assert.AreEqual(1, history.Count);
            Assert.AreEqual(CurrencyKind.Coins, history[0].Currency);
            Assert.AreEqual(250, history[0].Amount);
            Assert.AreEqual("battle_pass", history[0].Source);
        }

        [Test]
        public void Add_EventToken_TracksByTokenId()
        {
            _wallet.Add(CurrencyKind.EventToken, 50, "quest", "event_easter");
            _wallet.Add(CurrencyKind.EventToken, 80, "quest", "event_halloween");

            Assert.AreEqual(50, _wallet.GetBalance(CurrencyKind.EventToken, "event_easter"));
            Assert.AreEqual(80, _wallet.GetBalance(CurrencyKind.EventToken, "event_halloween"));
            Assert.AreEqual(0, _wallet.GetBalance(CurrencyKind.EventToken, "other"));
        }

        [Test]
        public void Add_ThrowsArgumentException_WhenSourceIsNullOrWhitespace()
        {
            Assert.Throws<ArgumentException>(() => _wallet.Add(CurrencyKind.Coins, 100, null));
            Assert.Throws<ArgumentException>(() => _wallet.Add(CurrencyKind.Coins, 100, ""));
            Assert.Throws<ArgumentException>(() => _wallet.Add(CurrencyKind.Coins, 100, "   "));
        }

        [Test]
        public void Add_ThrowsArgumentOutOfRangeException_WhenAmountIsZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(CurrencyKind.Coins, 0, "source"));
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.Add(CurrencyKind.Coins, -50, "source"));
        }

        [Test]
        public void TrySpend_Succeeds_WhenBalanceSufficient()
        {
            _wallet.Add(CurrencyKind.Medals, 10, "achievement");
            bool success = _wallet.TrySpend(CurrencyKind.Medals, 4, "unlock_skin");

            Assert.IsTrue(success);
            Assert.AreEqual(6, _wallet.GetBalance(CurrencyKind.Medals));
            Assert.AreEqual(2, _events.Count);
            Assert.AreEqual(-4, _events[1].Delta);
            Assert.AreEqual(6, _events[1].NewBalance);
            Assert.AreEqual("unlock_skin", _events[1].Source);

            IReadOnlyList<WalletTransaction> history = _wallet.GetHistory();
            Assert.AreEqual(2, history.Count);
            Assert.AreEqual(-4, history[1].Amount);
        }

        [Test]
        public void TrySpend_Fails_WhenBalanceInsufficient_LeavesStateUnchanged()
        {
            _wallet.Add(CurrencyKind.Recruits, 5, "rescue");
            bool success = _wallet.TrySpend(CurrencyKind.Recruits, 10, "squad_deploy");

            Assert.IsFalse(success);
            Assert.AreEqual(5, _wallet.GetBalance(CurrencyKind.Recruits));
            Assert.AreEqual(1, _events.Count);
            Assert.AreEqual(1, _wallet.GetHistory().Count);
        }

        [Test]
        public void TrySpend_ThrowsArgumentException_WhenSourceIsNullOrWhitespace()
        {
            _wallet.Add(CurrencyKind.Coins, 50, "init");
            Assert.Throws<ArgumentException>(() => _wallet.TrySpend(CurrencyKind.Coins, 10, null));
            Assert.Throws<ArgumentException>(() => _wallet.TrySpend(CurrencyKind.Coins, 10, " "));
        }

        [Test]
        public void TrySpend_ThrowsArgumentOutOfRangeException_WhenAmountIsZeroOrNegative()
        {
            _wallet.Add(CurrencyKind.Coins, 50, "init");
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend(CurrencyKind.Coins, 0, "shop"));
            Assert.Throws<ArgumentOutOfRangeException>(() => _wallet.TrySpend(CurrencyKind.Coins, -10, "shop"));
        }

        [Test]
        public void Wallet_WithoutEventBus_OperatesSilently()
        {
            var silentWallet = new Wallet();
            Assert.DoesNotThrow(() => silentWallet.Add(CurrencyKind.Coins, 100, "drop"));
            Assert.AreEqual(100, silentWallet.GetBalance(CurrencyKind.Coins));
            Assert.IsTrue(silentWallet.TrySpend(CurrencyKind.Coins, 50, "craft"));
            Assert.AreEqual(50, silentWallet.GetBalance(CurrencyKind.Coins));
        }
    }
}
