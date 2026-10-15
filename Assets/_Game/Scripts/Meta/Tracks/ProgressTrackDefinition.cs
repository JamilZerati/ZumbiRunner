using System.Collections.Generic;

namespace Game.Meta.Tracks
{
    public class ProgressTrackDefinition
    {
        public string TrackId { get; }
        public IReadOnlyList<MilestoneDefinition> Milestones { get; }
        public string RequiredEntitlementId { get; }

        public ProgressTrackDefinition(string trackId, IReadOnlyList<MilestoneDefinition> milestones, string requiredEntitlementId = null)
        {
            TrackId = trackId;
            Milestones = milestones;
            RequiredEntitlementId = requiredEntitlementId;
        }
    }
}
