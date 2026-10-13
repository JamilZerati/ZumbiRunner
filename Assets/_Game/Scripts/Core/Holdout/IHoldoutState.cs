namespace Game.Core.Holdout
{
    public enum HoldoutStatus
    {
        Inactive = 0,
        Defense = 1,
        Victory = 2,
        BarricadeDestroyed = 3
    }

    public interface IHoldoutState
    {
        string HoldoutId { get; }
        int MaxHp { get; }
        int CurrentHp { get; }
        int TotalSurvivors { get; }
        int SurvivorsSaved { get; }
        float DurationSeconds { get; }
        float TimeRemaining { get; }
        HoldoutStatus Status { get; }
        bool IsActive { get; }
        bool IsVictory { get; }
        bool IsBarricadeDestroyed { get; }
    }
}
