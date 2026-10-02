namespace Game.Core.Abilities
{
    public readonly struct AbilityTriggerContext
    {
        public int CurrentCharges { get; }
        public bool HasTargetsInLane { get; }
        public bool ManualTriggerRequested { get; }

        public AbilityTriggerContext(int currentCharges, bool hasTargetsInLane, bool manualTriggerRequested)
        {
            CurrentCharges = currentCharges;
            HasTargetsInLane = hasTargetsInLane;
            ManualTriggerRequested = manualTriggerRequested;
        }
    }
}
