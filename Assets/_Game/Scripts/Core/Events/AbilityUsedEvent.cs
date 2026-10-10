namespace Game.Core.Events
{
    public readonly struct AbilityUsedEvent
    {
        public string AbilityId { get; }
        public int RemainingCharges { get; }

        public AbilityUsedEvent(string abilityId, int remainingCharges)
        {
            AbilityId = abilityId;
            RemainingCharges = remainingCharges;
        }
    }
}
