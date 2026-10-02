using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class RangedSpitBehavior : IEnemyBehavior
    {
        public float Interval = 3f;
        public float WarningDuration = 1f;
        public int Damage = 30;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
