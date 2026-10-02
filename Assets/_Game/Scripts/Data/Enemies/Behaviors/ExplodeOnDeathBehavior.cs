using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class ExplodeOnDeathBehavior : IEnemyBehavior
    {
        public int Damage = 50;
        public float Radius = 3f;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
