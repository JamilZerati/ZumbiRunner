using System;

namespace Game.Core.Status
{
    [Serializable]
    public class FreezeStatus : IStatusEffect
    {
        public float Duration;
        public int Threshold;

        public StatusKind Kind => StatusKind.Freeze;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (host.Has(StatusKind.Frozen))
            {
                return;
            }

            state.Stacks += stacks;
            state.Remaining = Duration;

            if (state.Stacks >= Threshold)
            {
                host.Remove(StatusKind.Freeze);
                host.Apply(new StatusApplication(StatusKind.Frozen, 1, this));
            }
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
            state.Remaining -= deltaTime;
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }

        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
