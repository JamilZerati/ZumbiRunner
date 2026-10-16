using System;
using System.Collections.Generic;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class ProgressTrackTests
    {
        private Entitlements _entitlements;
        private Wallet _wallet;
        private RewardSink _sink;
        private ProgressTrackDefinition _definition;
        private ProgressTrack _track;

        [SetUp]
        public void SetUp()
        {
            _entitlements = new Entitlements();
            _wallet = new Wallet();
            _sink = new RewardSink(_wallet, _entitlements);

            var milestones = new[]
            {
                new MilestoneDefinition(0, 50,
                    new RewardBundle("track_m0_free", new[] { new CurrencyReward(CurrencyKind.Coins, 100) }),
                    new RewardBundle("track_m0_prem", new[] { new CurrencyReward(CurrencyKind.Medals, 5) })),
                new MilestoneDefinition(1, 150,
                    new RewardBundle("track_m1_free", new[] { new CurrencyReward(CurrencyKind.Coins, 200) }),
                    new RewardBundle("track_m1_prem", new[] { new CurrencyReward(CurrencyKind.Medals, 10) }))
            };

            _definition = new ProgressTrackDefinition("battlepass_s1", milestones, "pass_s1_entitlement");
            _track = new ProgressTrack(_definition, _entitlements);
        }

        [Test]
        public void InitialState_PointsZero_NoClaims()
        {
            Assert.AreEqual("battlepass_s1", _track.TrackId);
            Assert.AreEqual(0, _track.CurrentPoints);
            Assert.IsFalse(_track.CanClaimMilestone(0, isPremium: false));
            Assert.IsFalse(_track.CanClaimMilestone(0, isPremium: true));
        }

        [Test]
        public void AddPoints_IncreasesPoints_ThrowsOnNonPositive()
        {
            _track.AddPoints(75);
            Assert.AreEqual(75, _track.CurrentPoints);

            Assert.Throws<ArgumentOutOfRangeException>(() => _track.AddPoints(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => _track.AddPoints(-20));
        }

        [Test]
        public void CanClaimMilestone_RespectsPointsRequirement()
        {
            _track.AddPoints(40);
            Assert.IsFalse(_track.CanClaimMilestone(0, isPremium: false));

            _track.AddPoints(10);
            Assert.IsTrue(_track.CanClaimMilestone(0, isPremium: false));
            Assert.IsFalse(_track.CanClaimMilestone(1, isPremium: false));
        }

        [Test]
        public void PremiumMilestone_RequiresEntitlement()
        {
            _track.AddPoints(100);

            Assert.IsTrue(_track.CanClaimMilestone(0, isPremium: false));
            Assert.IsFalse(_track.CanClaimMilestone(0, isPremium: true));

            _entitlements.GrantEntitlement("pass_s1_entitlement");
            Assert.IsTrue(_track.CanClaimMilestone(0, isPremium: true));
        }

        [Test]
        public void ClaimMilestone_AppliesRewards_AndPreventsDoubleClaim()
        {
            _track.AddPoints(60);

            var bundle = _track.ClaimMilestone(0, isPremium: false, _sink);
            Assert.IsNotNull(bundle);
            Assert.AreEqual(100, _wallet.GetBalance(CurrencyKind.Coins));

            Assert.IsFalse(_track.CanClaimMilestone(0, isPremium: false));
            var secondClaim = _track.ClaimMilestone(0, isPremium: false, _sink);
            Assert.IsNull(secondClaim);
            Assert.AreEqual(100, _wallet.GetBalance(CurrencyKind.Coins));
        }
    }
}
