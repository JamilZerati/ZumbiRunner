using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class ExplodeOnContactBehavior : IEnemyBehavior
    {
        public int Damage = 30;
        public float Radius = 2f;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime) { }
        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }

        public void OnEngage(EnemyBehaviorContext context)
        {
            context.DamageDealtToSquad += Damage;
            context.Squad?.Remove(Damage);
            context.CurrentHealth = 0;
        }

        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
