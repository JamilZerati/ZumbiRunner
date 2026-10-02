using System;
using Game.Core;
using UnityEngine;

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

        public void OnDeath(EnemyBehaviorContext context)
        {
            context.DamageDealtToZombies += Damage;

            if (context.NearbyZombies == null)
            {
                return;
            }

            for (int i = 0; i < context.NearbyZombies.Count; i++)
            {
                var zombie = context.NearbyZombies[i];
                if (zombie != null && zombie.IsAlive)
                {
                    float dist = Vector3.Distance(context.Position, zombie.Position);
                    if (dist <= Radius)
                    {
                        zombie.CurrentHealth = Mathf.Max(0, zombie.CurrentHealth - Damage);
                    }
                }
            }
        }
    }
}
