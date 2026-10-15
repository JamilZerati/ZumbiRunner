using System;
using System.Collections.Generic;
using Game.Meta.Tracks;

namespace Game.Meta.Save
{
    public class MetaState
    {
        public int SaveVersion { get; set; } = 1;
        public long LastSeenUtcTicks { get; set; }
        public Dictionary<string, int> Currencies { get; set; } = new Dictionary<string, int>();
        public HashSet<string> ClaimedGrantIds { get; set; } = new HashSet<string>();
        public HashSet<string> Entitlements { get; set; } = new HashSet<string>();
        public Dictionary<string, int> StageStars { get; set; } = new Dictionary<string, int>();
        public HashSet<string> UnlockedStages { get; set; } = new HashSet<string>();
        public Dictionary<string, ProgressTrackState> TrackStates { get; set; } = new Dictionary<string, ProgressTrackState>();
        public string IntegrityHash { get; set; }
    }
}
