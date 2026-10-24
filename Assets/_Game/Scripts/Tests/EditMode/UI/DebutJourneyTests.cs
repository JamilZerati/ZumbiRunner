using System.Collections.Generic;
using Game.Meta.Debut;
using Game.Meta.Rewards;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    public class DebutJourneyTests
    {
        private sealed class FakeRewardSink : IRewardSink
        {
            private readonly HashSet<string> _granted = new();
            public IWallet Wallet => null;
            public IEntitlements Entitlements => null;

            public bool HasGranted(string grantId) => _granted.Contains(grantId);
            public void RecordGrant(string grantId) => _granted.Add(grantId);
            public void AddItem(string itemId, int quantity) { }
            public void UnlockContent(string contentId) { }
        }

        [Test]
        public void CT_6_DebutJourney_HasSevenDays_AndAllowsClaimingOnlyReachedDays()
        {
            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);
            var sink = new FakeRewardSink();

            Assert.AreEqual(7, service.Definition.Milestones.Count);

            track.AddPoints(1);
            Assert.IsTrue(service.CanClaimDay(track, dayIndex: 0, currentLoginDay: 1));
            Assert.IsFalse(service.CanClaimDay(track, dayIndex: 1, currentLoginDay: 1));

            bool claimed = service.ClaimDay(track, dayIndex: 0, currentLoginDay: 1, sink);
            Assert.IsTrue(claimed);
            Assert.IsTrue(sink.HasGranted("debut_day_1"));

            Assert.IsFalse(service.CanClaimDay(track, dayIndex: 0, currentLoginDay: 1));
            Assert.IsFalse(service.ClaimDay(track, dayIndex: 0, currentLoginDay: 1, sink));

            Assert.IsFalse(service.ClaimDay(track, dayIndex: 1, currentLoginDay: 1, sink));

            track.AddPoints(1);
            Assert.IsTrue(service.CanClaimDay(track, dayIndex: 1, currentLoginDay: 2));
            bool claimedDay2 = service.ClaimDay(track, dayIndex: 1, currentLoginDay: 2, sink);
            Assert.IsTrue(claimedDay2);
            Assert.IsTrue(sink.HasGranted("debut_day_2"));
        }

        [Test]
        public void CT_6_DebutJourney_RejectsNullArgumentsAndOutOfBoundsDaysSafely()
        {
            var service = new DebutJourneyService();
            var track = new ProgressTrack(service.Definition);
            var sink = new FakeRewardSink();

            track.AddPoints(1);

            Assert.IsFalse(service.CanClaimDay(null, 0, 1));
            Assert.IsFalse(service.CanClaimDay(track, -1, 1));
            Assert.IsFalse(service.CanClaimDay(track, 7, 7));

            Assert.IsFalse(service.ClaimDay(null, 0, 1, sink));
            Assert.IsFalse(service.ClaimDay(track, 0, 1, null));
            Assert.IsFalse(sink.HasGranted("debut_day_1"));

            Assert.IsFalse(service.ClaimDay(track, -1, 1, sink));
            Assert.IsFalse(service.ClaimDay(track, 7, 7, sink));

            var otherTrack = new ProgressTrack(new ProgressTrackDefinition("other_track", new List<MilestoneDefinition>()));
            Assert.IsFalse(service.CanClaimDay(otherTrack, 0, 1));
            Assert.IsFalse(service.ClaimDay(otherTrack, 0, 1, sink));

            Assert.IsTrue(service.ClaimDay(track, 0, 1, sink));
            Assert.IsTrue(sink.HasGranted("debut_day_1"));
        }
    }
}
