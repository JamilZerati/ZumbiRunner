namespace Game.Core.Events
{
    public readonly struct AllyAbilityFiredEvent
    {
        public string AllyId { get; }
        public string AbilityName { get; }

        public AllyAbilityFiredEvent(string allyId, string abilityName) =>
            (AllyId, AbilityName) = (allyId, abilityName);
    }
}
