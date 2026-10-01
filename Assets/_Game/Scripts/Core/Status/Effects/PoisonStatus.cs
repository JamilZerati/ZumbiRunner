using System;

namespace Game.Core.Status
{
    [Serializable]
    public class PoisonStatus : IStatusEffect
    {
        public float Duration;
        public float TickInterval;
        public int DamagePerTickPerStack;
        public int MaxStacks;
        public int ExplosionDamagePerStack;
        public float ExplosionRadius;

        public StatusKind Kind => StatusKind.Poison;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (state == null) return;
            state.Stacks = Math.Min(MaxStacks, state.Stacks + stacks);
            state.Remaining = Duration;
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
            if (state == null) return;

            if (TickInterval <= 0f)
            {
                state.Remaining -= deltaTime;
                return;
            }

            float step = Math.Min(deltaTime, state.Remaining);
            state.TickTimer += step;
            state.Remaining -= deltaTime;

            while (state.TickTimer >= TickInterval - 1e-4f)
            {
                state.TickTimer -= TickInterval;
                host.DealDamage(DamagePerTickPerStack * state.Stacks, DamageType.Poison, this);
            }
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
            if (state == null || state.Stacks <= 0) return;
            if (host.Neighborhood == null || host.Owner == null) return;

            int explosionDamage = ExplosionDamagePerStack * state.Stacks;
            var targets = host.Neighborhood.FindNearby(host.Owner, ExplosionRadius, int.MaxValue);
            if (targets == null || targets.Count == 0) return;

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target != null && target.IsAlive)
                {
                    target.ReceiveHit(new DamageInfo(explosionDamage, DamageType.Poison, this), null);
                }
            }
        }

        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
