using System;
using System.Collections.Generic;
using Game.Meta.Rewards;
using Game.Meta.Tracks;

namespace Game.Meta.Debut
{
    public enum DebutDayStatus
    {
        Locked,
        Available,
        Claimed
    }

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
            if (track == null || track.TrackId != _definition.TrackId || dayIndex < 0 || dayIndex >= _definition.Milestones.Count)
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
            if (track == null || sink == null)
            {
                return false;
            }

            if (!CanClaimDay(track, dayIndex, currentLoginDay))
            {
                return false;
            }

            var bundle = track.ClaimMilestone(dayIndex, isPremium: false, sink);
            return bundle != null;
        }

        public DebutDayStatus GetDayStatus(IProgressTrack track, int dayIndex, int currentLoginDay)
        {
            if (track == null || track.TrackId != _definition.TrackId || dayIndex < 0 || dayIndex >= _definition.Milestones.Count)
            {
                return DebutDayStatus.Locked;
            }

            if (track is ProgressTrack concreteTrack && concreteTrack.State?.ClaimedFreeMilestones?.Contains(dayIndex) == true)
            {
                return DebutDayStatus.Claimed;
            }

            if (CanClaimDay(track, dayIndex, currentLoginDay))
            {
                return DebutDayStatus.Available;
            }

            var milestone = _definition.Milestones[dayIndex];
            if (track.CurrentPoints >= milestone.RequiredPoints && !track.CanClaimMilestone(dayIndex, isPremium: false))
            {
                return DebutDayStatus.Claimed;
            }

            return DebutDayStatus.Locked;
        }

        public IReadOnlyList<DebutDayStatus> GetDaysStatus(IProgressTrack track, int currentLoginDay)
        {
            var count = _definition.Milestones.Count;
            var result = new DebutDayStatus[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = GetDayStatus(track, i, currentLoginDay);
            }
            return result;
        }
    }
}
