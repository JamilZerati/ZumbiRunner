using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class BossPingPongBehavior : IEnemyBehavior
    {
        public float MinLane = 0f;
        public float MaxLane = 2f;
        public float Speed = 2f;

        public float CurrentPosition { get; set; }
        public int Direction { get; set; } = 1;

        private bool _initialized;

        public void Initialize(float position, int direction = 1)
        {
            CurrentPosition = Mathf.Clamp(position, MinLane, MaxLane);
            Direction = direction >= 0 ? 1 : -1;
            _initialized = true;
        }

        public void UpdateBehavior(EnemyBehaviorContext context, float deltaTime)
        {
            if (context == null || deltaTime <= 0f)
            {
                return;
            }

            if (!_initialized)
            {
                CurrentPosition = Mathf.Clamp(CurrentPosition != 0f ? CurrentPosition : context.LaneIndex, MinLane, MaxLane);
                if (Direction == 0)
                {
                    Direction = 1;
                }
                _initialized = true;
            }

            CurrentPosition += Direction * Speed * deltaTime;

            if (CurrentPosition >= MaxLane)
            {
                CurrentPosition = MaxLane;
                Direction = -1;
            }
            else if (CurrentPosition <= MinLane)
            {
                CurrentPosition = MinLane;
                Direction = 1;
            }

            context.LaneIndex = Mathf.Clamp(Mathf.RoundToInt(CurrentPosition), Mathf.RoundToInt(MinLane), Mathf.RoundToInt(MaxLane));
            context.Position = new Vector3(CurrentPosition, context.Position.y, context.Position.z);

            if (context.BossState != null)
            {
                context.BossState.CurrentLane = context.LaneIndex;
            }
        }

        public void OnHit(ref DamageInfo hit, EnemyBehaviorContext context) { }
        public void OnEngage(EnemyBehaviorContext context) { }
        public void OnDeath(EnemyBehaviorContext context) { }
    }
}
