using System;
using System.Collections.Generic;
using Game.Meta.Rewards;
using Game.Meta.Tracks;

namespace Game.Meta.Debut
{
    public class DebutJourneyService
    {
        private readonly ProgressTrackDefinition _definition;

        public ProgressTrackDefinition Definition => _definition;

        public DebutJourneyService(ProgressTrackDefinition definition = null)
        {
            _definition = definition ?? DebutJourneyDefinition.CreateDefaultTrack();
        }

        public bool CanClaimDay(IProgressTrack track, int dayIndex, int currentLoginDay)
        {
            if (track == null || dayIndex < 0 || dayIndex >= _definition.Milestones.Count)
            {
                return false;
            }

            if (dayIndex >= currentLoginDay)
            {
                return false;
            }

            return track.CanClaimMilestone(dayIndex, isPremium: false);
        }

        public bool ClaimDay(IProgressTrack track, int dayIndex, int currentLoginDay, IRewardSink sink)
        {
            if (!CanClaimDay(track, dayIndex, currentLoginDay))
            {
                return false;
            }

            var bundle = track.ClaimMilestone(dayIndex, isPremium: false, sink);
            return bundle != null;
        }
    }
}
