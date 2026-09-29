using System;

namespace Game.Core.Status
{
    [Serializable]
    public class FrozenStatus : IStatusEffect
    {
        public float Duration;

        public StatusKind Kind => StatusKind.Frozen;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            state.Remaining = Duration;
            state.Stacks = 1;
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
            state.Remaining -= deltaTime;
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }

        public float MoveSpeedMultiplier(StatusState state) => 0f;
    }
}
