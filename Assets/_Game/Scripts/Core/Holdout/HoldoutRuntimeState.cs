using System;

namespace Game.Core.Holdout
{
    public sealed class HoldoutRuntimeState : IHoldoutState
    {
        public string HoldoutId { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int TotalSurvivors { get; }
        public int SurvivorsSaved { get; private set; }
        public float DurationSeconds { get; }
        public float TimeRemaining { get; private set; }
        public HoldoutStatus Status { get; private set; }

        public bool IsActive => Status == HoldoutStatus.Defense;
        public bool IsVictory => Status == HoldoutStatus.Victory;
        public bool IsBarricadeDestroyed => Status == HoldoutStatus.BarricadeDestroyed;

        public HoldoutRuntimeState(string holdoutId, int maxHp, int totalSurvivors, float durationSeconds)
        {
            HoldoutId = holdoutId ?? string.Empty;
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            TotalSurvivors = Math.Max(0, totalSurvivors);
            SurvivorsSaved = 0;
            DurationSeconds = Math.Max(0f, durationSeconds);
            TimeRemaining = DurationSeconds;
            Status = HoldoutStatus.Defense;
        }

        public void StartDefense()
        {
            Status = HoldoutStatus.Defense;
            TimeRemaining = DurationSeconds;
            CurrentHp = MaxHp;
            SurvivorsSaved = 0;
        }

        public bool TakeDamage(int damage)
        {
            if (Status != HoldoutStatus.Defense || damage <= 0)
            {
                return false;
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
            if (CurrentHp == 0)
            {
                Status = HoldoutStatus.BarricadeDestroyed;
                SurvivorsSaved = 0;
                return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            if (Status != HoldoutStatus.Defense || deltaTime <= 0f)
            {
                return;
            }

            TimeRemaining = Math.Max(0f, TimeRemaining - deltaTime);
            if (TimeRemaining <= 0f)
            {
                ConcludeHoldout(true);
            }
        }

        public void ConcludeHoldout(bool timeoutOrCleared)
        {
            if (Status != HoldoutStatus.Defense)
            {
                return;
            }

            if (CurrentHp > 0 && timeoutOrCleared)
            {
                Status = HoldoutStatus.Victory;
                SurvivorsSaved = (int)Math.Ceiling(TotalSurvivors * ((double)CurrentHp / MaxHp));
            }
            else
            {
                Status = HoldoutStatus.BarricadeDestroyed;
                SurvivorsSaved = 0;
            }
        }
    }
}
