using System;

namespace Game.Core.Rescue
{
    public sealed class CageRuntimeState : ICageState
    {
        public string CageId { get; }
        public int LaneIndex { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public int SurvivorCount { get; }
        public string LostEnemyArchetypeId { get; }
        public bool IsOpen { get; private set; }
        public bool IsLost { get; private set; }

        public CageRuntimeState(string cageId, int laneIndex, int maxHp, int survivorCount, string lostEnemyArchetypeId = "walker")
        {
            CageId = cageId ?? string.Empty;
            LaneIndex = laneIndex;
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            SurvivorCount = Math.Max(1, survivorCount);
            LostEnemyArchetypeId = string.IsNullOrEmpty(lostEnemyArchetypeId) ? "walker" : lostEnemyArchetypeId;
            IsOpen = false;
            IsLost = false;
        }

        public bool TakeDamage(int damage)
        {
            if (IsOpen || IsLost || damage <= 0)
            {
                return false;
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
            if (CurrentHp == 0)
            {
                IsOpen = true;
                return true;
            }

            return false;
        }

        public bool MarkLost()
        {
            if (IsOpen || IsLost)
            {
                return false;
            }

            IsLost = true;
            return true;
        }
    }
}
