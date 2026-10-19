using System;

namespace Game.Core.Boss
{
    public class BossCombatRuntimeState
    {
        public string BossId { get; private set; }
        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }
        public bool IsAlive => CurrentHp > 0;
        public bool IsArenaActive { get; private set; }
        public int CurrentLane { get; set; }
        public int TargetTelegraphLane { get; private set; }
        public bool IsTelegraphing { get; private set; }
        public float TelegraphRemaining { get; private set; }

        public event Action<int> OnTelegraphExpired;
        public event Action OnBossDied;

        public void StartArena(string bossId, int maxHp, int initialLane = 0)
        {
            if (string.IsNullOrWhiteSpace(bossId))
            {
                throw new ArgumentException("BossId is required.", nameof(bossId));
            }

            BossId = bossId;
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            CurrentLane = initialLane;
            IsArenaActive = true;
            IsTelegraphing = false;
            TelegraphRemaining = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (!IsArenaActive || !IsAlive)
            {
                return;
            }

            if (IsTelegraphing)
            {
                TelegraphRemaining -= deltaTime;
                if (TelegraphRemaining <= 0f)
                {
                    IsTelegraphing = false;
                    TelegraphRemaining = 0f;
                    OnTelegraphExpired?.Invoke(TargetTelegraphLane);
                }
            }
        }

        public int ApplyDamage(int damage)
        {
            if (!IsArenaActive || !IsAlive || damage <= 0)
            {
                return 0;
            }

            int dealt = Math.Min(damage, CurrentHp);
            CurrentHp -= dealt;

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                OnBossDied?.Invoke();
            }

            return dealt;
        }

        public void TriggerTelegraph(int lane, float duration)
        {
            if (!IsArenaActive || !IsAlive)
            {
                return;
            }

            TargetTelegraphLane = lane;
            TelegraphRemaining = Math.Max(0.1f, duration);
            IsTelegraphing = true;
        }

        public void EndArena()
        {
            IsArenaActive = false;
            IsTelegraphing = false;
            TelegraphRemaining = 0f;
        }
    }
}
