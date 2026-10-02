using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class HealAuraBehavior : IEnemyBehavior
    {
        public int HealPerSecond = 5;
        public float Radius = 4f;

        private float _accumulator;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null)
            {
                return;
            }

            _accumulator += HealPerSecond * deltaTime;
            int healToApply = (int)_accumulator;

            if (healToApply > 0)
            {
                _accumulator -= healToApply;
                context.HealApplied += healToApply;

                if (context.NearbyZombies != null)
                {
                    for (int i = 0; i < context.NearbyZombies.Count; i++)
                    {
                        var zombie = context.NearbyZombies[i];
                        if (zombie != null && zombie.IsAlive)
                        {
                            float dist = Vector3.Distance(context.Position, zombie.Position);
                            if (dist <= Radius)
                            {
                                zombie.CurrentHealth = Mathf.Min(zombie.MaxHealth, zombie.CurrentHealth + healToApply);
                            }
                        }
                    }
                }
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
