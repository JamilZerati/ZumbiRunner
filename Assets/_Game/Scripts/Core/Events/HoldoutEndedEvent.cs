namespace Game.Core.Events
{
    public readonly struct HoldoutEndedEvent
    {
        public string HoldoutId { get; }
        public int SurvivorsSaved { get; }
        public int RemainingHp { get; }
        public bool BarricadeHeld { get; }

        public HoldoutEndedEvent(string holdoutId, int survivorsSaved, int remainingHp, bool barricadeHeld)
        {
            HoldoutId = holdoutId ?? string.Empty;
            SurvivorsSaved = System.Math.Max(0, survivorsSaved);
            RemainingHp = System.Math.Max(0, remainingHp);
            BarricadeHeld = barricadeHeld;
        }
    }
}
