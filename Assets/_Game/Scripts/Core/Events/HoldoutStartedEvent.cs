namespace Game.Core.Events
{
    public readonly struct HoldoutStartedEvent
    {
        public string HoldoutId { get; }
        public int MaxHp { get; }
        public int TotalSurvivors { get; }
        public float DurationSeconds { get; }

        public HoldoutStartedEvent(string holdoutId, int maxHp, int totalSurvivors, float durationSeconds)
        {
            HoldoutId = holdoutId ?? string.Empty;
            MaxHp = System.Math.Max(1, maxHp);
            TotalSurvivors = System.Math.Max(0, totalSurvivors);
            DurationSeconds = System.Math.Max(0f, durationSeconds);
        }
    }
}
