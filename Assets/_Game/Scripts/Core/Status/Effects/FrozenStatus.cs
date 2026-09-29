using System;

namespace Game.Core.Status
{
    [Serializable]
    public class FrozenStatus : IStatusEffect
    {
        public float Duration;

        public StatusKind Kind => StatusKind.Frozen;
        public bool IsPersistent => true;
        public void OnApply(IStatusHost host, StatusState state, int stacks) => throw new NotImplementedException();
        public void OnTick(IStatusHost host, StatusState state, float deltaTime) => throw new NotImplementedException();
        public void OnHostDied(IStatusHost host, StatusState state) => throw new NotImplementedException();
        public float MoveSpeedMultiplier(StatusState state) => throw new NotImplementedException();
    }
}
