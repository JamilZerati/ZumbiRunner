using System;
using System.Collections.Generic;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class RewardTests
    {
        private Wallet _wallet;
        private FakeEntitlements _entitlements;
        private RewardSink _sink;

        [SetUp]
        public void SetUp()
        {
            _wallet = new Wallet();
            _entitlements = new FakeEntitlements();
            _sink = new RewardSink(_wallet, _entitlements);
        }

        [Test]
        public void RewardBundle_ThrowsArgumentNullException_WhenSinkIsNull()
        {
            var bundle = new RewardBundle("grant_1", Array.Empty<IReward>());
            Assert.Throws<ArgumentNullException>(() => bundle.Grant(null));
        }

        [Test]
        public void RewardBundle_Grant_AppliesAllRewards_AndRecordsGrantId()
        {
            var bundle = new RewardBundle("level_1_reward", new IReward[]
            {
                new CurrencyReward(CurrencyKind.Coins, 200),
                new ItemReward("medkit", 2),
                new UnlockReward("skin_veteran"),
                new EntitlementReward("pass_premium")
            });

            bool granted = bundle.Grant(_sink);

            Assert.IsTrue(granted);
            Assert.IsTrue(_sink.HasGranted("level_1_reward"));
            Assert.AreEqual(200, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(2, _sink.GetItemQuantity("medkit"));
            Assert.IsTrue(_sink.IsContentUnlocked("skin_veteran"));
            Assert.IsTrue(_entitlements.HasEntitlement("pass_premium"));
        }

        [Test]
        public void RewardBundle_Grant_IsIdempotent_ReexecutionReturnsFalseWithoutDuplication()
        {
            var bundle = new RewardBundle("daily_login_day1", new IReward[]
            {
                new CurrencyReward(CurrencyKind.Coins, 50),
                new ItemReward("grenade", 1)
            });

            bool first = bundle.Grant(_sink);
            bool second = bundle.Grant(_sink);

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.AreEqual(50, _wallet.GetBalance(CurrencyKind.Coins));
            Assert.AreEqual(1, _sink.GetItemQuantity("grenade"));
        }

        [Test]
        public void RewardBundle_DifferentGrantIds_CanBeGrantedIndependently()
        {
            var bundle1 = new RewardBundle("quest_01", new[] { new CurrencyReward(CurrencyKind.Medals, 1) });
            var bundle2 = new RewardBundle("quest_02", new[] { new CurrencyReward(CurrencyKind.Medals, 2) });

            Assert.IsTrue(bundle1.Grant(_sink));
            Assert.IsTrue(bundle2.Grant(_sink));
            Assert.AreEqual(3, _wallet.GetBalance(CurrencyKind.Medals));
        }

        [Test]
        public void CurrencyReward_WithEventToken_AppliesTokenId()
        {
            var bundle = new RewardBundle("event_drop_1", new[]
            {
                new CurrencyReward(CurrencyKind.EventToken, 25, "zombie_event")
            });

            bundle.Grant(_sink);

            Assert.AreEqual(25, _wallet.GetBalance(CurrencyKind.EventToken, "zombie_event"));
        }

        private sealed class FakeEntitlements : IEntitlements
        {
            private readonly HashSet<string> _granted = new();

            public bool HasEntitlement(string entitlementId) => _granted.Contains(entitlementId);
            public void GrantEntitlement(string entitlementId) => _granted.Add(entitlementId);
            public void RevokeEntitlement(string entitlementId) => _granted.Remove(entitlementId);
            public IReadOnlyCollection<string> GetAllEntitlements() => _granted;
        }
    }
}
