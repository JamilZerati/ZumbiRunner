using System;

namespace Game.Core.Status
{
    [Serializable]
    public class SlowStatus : IStatusEffect
    {
        public float Duration;
        public float SlowPercent;

        public StatusKind Kind => StatusKind.Slow;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (state == null) return;
            state.Potency = Math.Max(state.Potency, SlowPercent);
            state.Remaining = Duration;
            state.Stacks = 1;
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
            if (state == null) return;
            state.Remaining -= deltaTime;
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }

        public float MoveSpeedMultiplier(StatusState state)
        {
            if (state == null) return 1f;
            return Math.Max(0f, 1f - state.Potency);
        }
    }
}
