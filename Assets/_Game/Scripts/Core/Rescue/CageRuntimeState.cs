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
        public CageStatus Status { get; private set; }
        public bool IsOpen => Status == CageStatus.Opened;
        public bool IsLost => Status == CageStatus.Lost;
        public bool IsIntact => Status == CageStatus.Intact;

        public CageRuntimeState(string cageId, int laneIndex, int maxHp, int survivorCount, string lostEnemyArchetypeId = "walker")
        {
            CageId = cageId ?? string.Empty;
            LaneIndex = laneIndex;
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = MaxHp;
            SurvivorCount = Math.Max(1, survivorCount);
            LostEnemyArchetypeId = string.IsNullOrEmpty(lostEnemyArchetypeId) ? "walker" : lostEnemyArchetypeId;
            Status = CageStatus.Intact;
        }

        public bool TakeDamage(int damage)
        {
            if (Status != CageStatus.Intact || damage <= 0)
            {
                return false;
            }

            CurrentHp = Math.Max(0, CurrentHp - damage);
            if (CurrentHp == 0)
            {
                Status = CageStatus.Opened;
                return true;
            }

            return false;
        }

        public bool MarkLost()
        {
            if (Status != CageStatus.Intact)
            {
                return false;
            }

            Status = CageStatus.Lost;
            return true;
        }
    }
}
