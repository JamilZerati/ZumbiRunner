using Game.Meta.Rewards;

namespace Game.Meta.Tracks
{
    public interface IProgressTrack
    {
        string TrackId { get; }
        int CurrentPoints { get; }
        void AddPoints(int points);
        bool CanClaimMilestone(int milestoneIndex, bool isPremium);
        RewardBundle ClaimMilestone(int milestoneIndex, bool isPremium, IRewardSink sink);
    }
}
