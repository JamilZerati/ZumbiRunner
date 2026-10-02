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

        private float _timer;

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null)
            {
                return;
            }

            _timer += deltaTime;

            if (_timer >= Interval - WarningDuration && _timer < Interval)
            {
                if (!context.SpitWarningActive)
                {
                    context.SpitWarningActive = true;
                    context.SpitWarningLane = context.GeneralLane;
                }
            }

            if (_timer >= Interval)
            {
                context.DidSpit = true;

                if (context.GeneralLane == context.SpitWarningLane)
                {
                    context.DamageDealtToSquad += Damage;
                    context.Squad?.Remove(Damage);
                }

                _timer = 0f;
                context.SpitWarningActive = false;
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
