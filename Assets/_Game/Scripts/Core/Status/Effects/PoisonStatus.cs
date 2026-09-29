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
        public void OnApply(IStatusHost host, StatusState state, int stacks) => throw new NotImplementedException();
        public void OnTick(IStatusHost host, StatusState state, float deltaTime) => throw new NotImplementedException();
        public void OnHostDied(IStatusHost host, StatusState state) => throw new NotImplementedException();
        public float MoveSpeedMultiplier(StatusState state) => throw new NotImplementedException();
    }
}
