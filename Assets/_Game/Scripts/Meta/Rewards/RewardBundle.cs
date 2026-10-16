using System;
using System.Collections.Generic;

namespace Game.Meta.Rewards
{
    public class RewardBundle
    {
        public string GrantId { get; }
        public IReadOnlyList<IReward> Rewards { get; }

        public RewardBundle(string grantId, IReadOnlyList<IReward> rewards)
        {
            GrantId = grantId;
            Rewards = rewards ?? Array.Empty<IReward>();
        }

        public bool Grant(IRewardSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            if (sink.HasGranted(GrantId)) return false;
            foreach (var reward in Rewards)
            {
                reward.Apply(sink);
            }
            sink.RecordGrant(GrantId);
            return true;
        }
    }
}
