using System;
using Game.Core;

namespace Game.Data
{
    [Serializable]
    public class ResurrectBehavior : IEnemyBehavior
    {
        public float Interval = 6f;
        public string ArchetypeId = "walker";

        private float _timer;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null)
            {
                return;
            }

            _timer += deltaTime;

            if (_timer >= Interval)
            {
                _timer = 0f;
                context.ResurrectTriggered = true;
                context.ResurrectArchetypeId = ArchetypeId;
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
