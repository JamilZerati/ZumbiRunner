namespace Game.Core.Events
{
    public readonly struct AllyRecruitedEvent
    {
        public string AllyId { get; }
        public string DisplayName { get; }
        public bool IsPet { get; }
        public bool WasRestored { get; }

        public AllyRecruitedEvent(string allyId, string displayName, bool isPet, bool wasRestored) =>
            (AllyId, DisplayName, IsPet, WasRestored) = (allyId, displayName, isPet, wasRestored);
    }
}
