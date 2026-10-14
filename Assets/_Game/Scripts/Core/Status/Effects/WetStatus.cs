using System;

namespace Game.Core.Status
{
    [Serializable]
    public class WetStatus : IStatusEffect
    {
        public float Duration;

        public StatusKind Kind => StatusKind.Wet;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (state == null) return;
            state.Remaining = Duration;
            state.Stacks = Math.Max(1, stacks);
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
            if (state == null) return;
            state.Remaining -= deltaTime;
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }
        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
