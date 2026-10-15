using System;
using Game.Meta.Rewards;

namespace Game.Meta.Tracks
{
    public class ProgressTrack : IProgressTrack
    {
        public string TrackId => throw new NotImplementedException();
        public int CurrentPoints => throw new NotImplementedException();
        public void AddPoints(int points) => throw new NotImplementedException();
        public bool CanClaimMilestone(int milestoneIndex, bool isPremium) => throw new NotImplementedException();
        public RewardBundle ClaimMilestone(int milestoneIndex, bool isPremium, IRewardSink sink) => throw new NotImplementedException();
    }
}
