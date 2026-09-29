using System;

namespace Game.Core.Status
{
    [Serializable]
    public class BurnStatus : IStatusEffect
    {
        public float Duration;
        public float TickInterval;
        public int DamagePerTick;

        public StatusKind Kind => StatusKind.Burn;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (state == null) return;
            state.Remaining = Duration;
            state.Stacks = 1;
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
                host.DealDamage(DamagePerTick, DamageType.Fire, this);
            }
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }

        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
