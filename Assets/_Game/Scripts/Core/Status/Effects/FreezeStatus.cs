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
        public void OnApply(IStatusHost host, StatusState state, int stacks) => throw new NotImplementedException();
        public void OnTick(IStatusHost host, StatusState state, float deltaTime) => throw new NotImplementedException();
        public void OnHostDied(IStatusHost host, StatusState state) => throw new NotImplementedException();
        public float MoveSpeedMultiplier(StatusState state) => throw new NotImplementedException();
    }
}
