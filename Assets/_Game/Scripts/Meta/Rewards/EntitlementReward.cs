using System;

namespace Game.Meta.Rewards
{
    public class EntitlementReward : IReward
    {
        public string RewardType => "Entitlement";
        public string EntitlementId { get; }

        public EntitlementReward(string entitlementId)
        {
            EntitlementId = entitlementId;
        }

        public void Apply(IRewardSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.Entitlements?.GrantEntitlement(EntitlementId);
        }
    }
}
