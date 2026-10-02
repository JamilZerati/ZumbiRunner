using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class HealAuraBehavior : IEnemyBehavior
    {
        public int HealPerSecond = 5;
        public float Radius = 4f;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
