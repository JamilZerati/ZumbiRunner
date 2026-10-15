using System;
using System.Collections.Generic;

namespace Game.Meta.Tracks
{
    public class Entitlements : IEntitlements
    {
        public bool HasEntitlement(string entitlementId) => throw new NotImplementedException();
        public void GrantEntitlement(string entitlementId) => throw new NotImplementedException();
        public void RevokeEntitlement(string entitlementId) => throw new NotImplementedException();
        public IReadOnlyCollection<string> GetAllEntitlements() => throw new NotImplementedException();
    }
}
