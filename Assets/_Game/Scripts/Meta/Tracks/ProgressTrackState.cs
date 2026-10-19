using System.Collections.Generic;

namespace Game.Meta.Tracks
{
    public class ProgressTrackState
    {
        public string TrackId { get; set; }
        public int CurrentPoints { get; set; }
        public HashSet<int> ClaimedFreeMilestones { get; set; } = new HashSet<int>();
        public HashSet<int> ClaimedPremiumMilestones { get; set; } = new HashSet<int>();
    }
}
