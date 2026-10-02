using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class ChaseLaneBehavior : IEnemyBehavior
    {
        public float Delay = 1f;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
