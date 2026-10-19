using System;
using System.Collections.Generic;

namespace Game.Meta.Tracks
{
    public class Entitlements : IEntitlements
    {
        private readonly HashSet<string> _entitlements = new(StringComparer.OrdinalIgnoreCase);

        public bool HasEntitlement(string entitlementId)
        {
            if (string.IsNullOrEmpty(entitlementId)) return false;
            return _entitlements.Contains(entitlementId);
        }

        public void GrantEntitlement(string entitlementId)
        {
            if (string.IsNullOrWhiteSpace(entitlementId))
            {
                throw new ArgumentException("EntitlementId is required.", nameof(entitlementId));
            }
            _entitlements.Add(entitlementId);
        }

        public void RevokeEntitlement(string entitlementId)
        {
            if (string.IsNullOrEmpty(entitlementId)) return;
            _entitlements.Remove(entitlementId);
        }

        public IReadOnlyCollection<string> GetAllEntitlements() => _entitlements;
    }
}
