using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class ChaseLaneBehavior : IEnemyBehavior
    {
        public float Delay = 1f;

        private float _elapsed;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null)
            {
                return;
            }

            if (context.LaneIndex != context.GeneralLane)
            {
                _elapsed += deltaTime;
                if (_elapsed >= Delay)
                {
                    context.LaneIndex = context.GeneralLane;
                    _elapsed = 0f;
                }
            }
            else
            {
                _elapsed = 0f;
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
