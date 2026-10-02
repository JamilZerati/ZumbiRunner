using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class FrontShieldBehavior : IEnemyBehavior
    {
        public bool IsActive = true;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
