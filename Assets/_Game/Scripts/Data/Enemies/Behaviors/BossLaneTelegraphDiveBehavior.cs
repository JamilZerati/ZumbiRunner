using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class BossLaneTelegraphDiveBehavior : IEnemyBehavior
    {
        public float Interval = 4f;
        public float TelegraphDuration = 1f;
        public int Damage = 50;

        public bool IsTelegraphing { get; private set; }
        public int TargetLane { get; private set; }
        public float TelegraphRemaining { get; private set; }

        public event Action<int> OnTelegraphStarted;
        public event Action<int, int> OnDiveExecuted;

        private float _timer;

        public void ResetTimer()
        {
            _timer = 0f;
            IsTelegraphing = false;
            TelegraphRemaining = 0f;
        }

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null || deltaTime <= 0f)
            {
                return;
            }

            _timer += deltaTime;

            if (_timer >= Interval - TelegraphDuration && _timer < Interval)
            {
                if (!IsTelegraphing)
                {
                    IsTelegraphing = true;
                    TargetLane = context.GeneralLane;
                    TelegraphRemaining = Interval - _timer;
                    context.BossTelegraphActive = true;
                    context.BossTelegraphLane = TargetLane;
                    context.BossState?.TriggerTelegraph(TargetLane, TelegraphDuration);
                    OnTelegraphStarted?.Invoke(TargetLane);
                }
                else
                {
                    TelegraphRemaining = Mathf.Max(0f, Interval - _timer);
                }
            }

            if (_timer >= Interval)
            {
                if (!IsTelegraphing)
                {
                    TargetLane = context.GeneralLane;
                }

                IsTelegraphing = false;
                TelegraphRemaining = 0f;
                context.BossTelegraphActive = false;
                context.BossDidDive = true;

                if (context.GeneralLane == TargetLane)
                {
                    context.DamageDealtToSquad += Damage;
                    context.Squad?.Remove(Damage);
                }

                OnDiveExecuted?.Invoke(TargetLane, Damage);
                _timer = 0f;
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
