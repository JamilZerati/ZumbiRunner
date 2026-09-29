using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;

namespace Game.Core.Status
{
    public sealed class StatusEffectController : IStatusHost
    {
        public StatusEffectController(IDamageable health, IStatusReceiver owner,
                                      IStatusCatalog catalog = null, IEffectInteractionTable interactions = null,
                                      IStatusNeighborhood neighborhood = null, IEventBus eventBus = null)
        {
        }

        public float MoveSpeedMultiplier => throw new NotImplementedException();
        public bool IsStunned => throw new NotImplementedException();
        public void Tick(float deltaTime) => throw new NotImplementedException();
        public int ResolveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit) => throw new NotImplementedException();
        public void Clear() => throw new NotImplementedException();

        public IStatusReceiver Owner => throw new NotImplementedException();
        public IStatusNeighborhood Neighborhood => throw new NotImplementedException();
        public IStatusCatalog Catalog => throw new NotImplementedException();
        public bool Has(StatusKind kind) => throw new NotImplementedException();
        public int GetStacks(StatusKind kind) => throw new NotImplementedException();
        public void Apply(StatusApplication application) => throw new NotImplementedException();
        public bool Remove(StatusKind kind) => throw new NotImplementedException();
        public void DealDamage(int amount, DamageType type, object source) => throw new NotImplementedException();
    }
}
