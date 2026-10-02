using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class FrontShieldBehavior : IEnemyBehavior
    {
        public bool IsActive = true;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context)
        {
            if (!IsActive || !context.ShieldActive)
            {
                return;
            }

            if (hit.Type != DamageType.Physical)
            {
                IsActive = false;
                context.ShieldActive = false;
                return;
            }

            hit = new DamageInfo(0, hit.Type, hit.Source);
        }

        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
