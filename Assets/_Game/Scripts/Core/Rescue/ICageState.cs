namespace Game.Core.Rescue
{
    public interface ICageState
    {
        string CageId { get; }
        int LaneIndex { get; }
        int MaxHp { get; }
        int CurrentHp { get; }
        int SurvivorCount { get; }
        string LostEnemyArchetypeId { get; }
        bool IsOpen { get; }
        bool IsLost { get; }

        bool TakeDamage(int damage);
        bool MarkLost();
    }
}
