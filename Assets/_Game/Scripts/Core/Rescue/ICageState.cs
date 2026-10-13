namespace Game.Core.Rescue
{
    public enum CageStatus
    {
        Intact = 0,
        Opened = 1,
        Lost = 2
    }

    public interface ICageState
    {
        string CageId { get; }
        int LaneIndex { get; }
        int MaxHp { get; }
        int CurrentHp { get; }
        int SurvivorCount { get; }
        string LostEnemyArchetypeId { get; }
        CageStatus Status { get; }
        bool IsOpen { get; }
        bool IsLost { get; }
        bool IsIntact { get; }

        bool TakeDamage(int damage);
        bool MarkLost();
    }
}
