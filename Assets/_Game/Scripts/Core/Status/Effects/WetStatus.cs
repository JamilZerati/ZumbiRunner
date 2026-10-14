using System;

namespace Game.Core.Status
{
    [Serializable]
    public class WetStatus : IStatusEffect
    {
        public float Duration;

        public StatusKind Kind => StatusKind.Wet;
        public bool IsPersistent => true;

        public void OnApply(IStatusHost host, StatusState state, int stacks) => throw new NotImplementedException();
        public void OnTick(IStatusHost host, StatusState state, float deltaTime) => throw new NotImplementedException();
        public void OnHostDied(IStatusHost host, StatusState state) { }
        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
