using System.Collections.Generic;

namespace Game.Meta.Tracks
{
    public interface IEntitlements
    {
        bool HasEntitlement(string entitlementId);
        void GrantEntitlement(string entitlementId);
        void RevokeEntitlement(string entitlementId);
        IReadOnlyCollection<string> GetAllEntitlements();
    }
}
