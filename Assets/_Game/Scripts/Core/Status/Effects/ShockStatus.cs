using System;

namespace Game.Core.Status
{
    [Serializable]
    public class ShockStatus : IStatusEffect
    {
        public int ChainCount;
        public float ChainRadius;
        public int ChainDamage;

        public StatusKind Kind => StatusKind.Shock;
        public bool IsPersistent => false;

        public void OnApply(IStatusHost host, StatusState state, int stacks)
        {
            if (host.Neighborhood == null || host.Owner == null)
            {
                return;
            }

            var targets = host.Neighborhood.FindNearby(host.Owner, ChainRadius, ChainCount);
            if (targets == null || targets.Count == 0)
            {
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target != null && target.IsAlive)
                {
                    target.ReceiveHit(new DamageInfo(ChainDamage, DamageType.Lightning, this), null);
                }
            }
        }

        public void OnTick(IStatusHost host, StatusState state, float deltaTime)
        {
        }

        public void OnHostDied(IStatusHost host, StatusState state)
        {
        }

        public float MoveSpeedMultiplier(StatusState state) => 1f;
    }
}
