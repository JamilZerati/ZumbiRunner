using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class MoveStraightBehavior : IEnemyBehavior
    {
        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null || context.IsStopped || context.IsEngaged)
            {
                return;
            }

            context.Position += Vector3.back * (context.Speed * deltaTime);
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
