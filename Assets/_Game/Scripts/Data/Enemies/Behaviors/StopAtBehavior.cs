using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class StopAtBehavior : IEnemyBehavior
    {
        public float Distance = 25f;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null)
            {
                return;
            }

            context.IsStopped = context.DistanceToTarget <= Distance;
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
