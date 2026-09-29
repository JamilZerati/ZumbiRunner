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
        public void OnApply(IStatusHost host, StatusState state, int stacks) => throw new NotImplementedException();
        public void OnTick(IStatusHost host, StatusState state, float deltaTime) => throw new NotImplementedException();
        public void OnHostDied(IStatusHost host, StatusState state) => throw new NotImplementedException();
        public float MoveSpeedMultiplier(StatusState state) => throw new NotImplementedException();
    }
}
