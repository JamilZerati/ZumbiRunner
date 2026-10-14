namespace Game.Core.Events
{
    public readonly struct AllyKnockedOutEvent
    {
        public string AllyId { get; }

        public AllyKnockedOutEvent(string allyId) => AllyId = allyId;
    }
}
