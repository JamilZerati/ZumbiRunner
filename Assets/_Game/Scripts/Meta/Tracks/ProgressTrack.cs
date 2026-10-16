using System;
using System.Collections.Generic;
using System.Linq;
using Game.Meta.Rewards;

namespace Game.Meta.Tracks
{
    public class ProgressTrack : IProgressTrack
    {
        private readonly ProgressTrackDefinition _definition;
        private readonly IEntitlements _entitlements;
        private readonly ProgressTrackState _state;

        public string TrackId => _definition.TrackId;
        public int CurrentPoints => _state.CurrentPoints;
        public ProgressTrackState State => _state;

        public ProgressTrack(ProgressTrackDefinition definition, IEntitlements entitlements = null, ProgressTrackState state = null)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _entitlements = entitlements;
            _state = state ?? new ProgressTrackState
            {
                TrackId = definition.TrackId,
                CurrentPoints = 0
            };
        }

        public void AddPoints(int points)
        {
            if (points <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(points), "Points must be positive.");
            }
            _state.CurrentPoints += points;
        }

        public bool CanClaimMilestone(int milestoneIndex, bool isPremium)
        {
            var milestone = GetMilestone(milestoneIndex);
            if (milestone == null) return false;

            if (_state.CurrentPoints < milestone.RequiredPoints)
            {
                return false;
            }

            if (!isPremium)
            {
                if (_state.ClaimedFreeMilestones.Contains(milestoneIndex)) return false;
                return milestone.FreeReward != null;
            }
            else
            {
                if (_state.ClaimedPremiumMilestones.Contains(milestoneIndex)) return false;
                if (milestone.PremiumReward == null) return false;

                if (!string.IsNullOrEmpty(_definition.RequiredEntitlementId))
                {
                    if (_entitlements == null || !_entitlements.HasEntitlement(_definition.RequiredEntitlementId))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public RewardBundle ClaimMilestone(int milestoneIndex, bool isPremium, IRewardSink sink)
        {
            if (!CanClaimMilestone(milestoneIndex, isPremium))
            {
                return null;
            }

            var milestone = GetMilestone(milestoneIndex);
            var bundle = isPremium ? milestone.PremiumReward : milestone.FreeReward;

            if (bundle != null && sink != null)
            {
                bundle.Grant(sink);
            }

            if (isPremium)
            {
                _state.ClaimedPremiumMilestones.Add(milestoneIndex);
            }
            else
            {
                _state.ClaimedFreeMilestones.Add(milestoneIndex);
            }

            return bundle;
        }

        private MilestoneDefinition GetMilestone(int index)
        {
            if (_definition.Milestones == null) return null;
            return _definition.Milestones.FirstOrDefault(m => m.Index == index);
        }
    }
}
