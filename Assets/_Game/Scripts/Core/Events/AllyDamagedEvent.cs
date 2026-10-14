namespace Game.Core.Events
{
    public readonly struct AllyDamagedEvent
    {
        public string AllyId { get; }
        public int DamageTaken { get; }
        public int RemainingHp { get; }

        public AllyDamagedEvent(string allyId, int damageTaken, int remainingHp) =>
            (AllyId, DamageTaken, RemainingHp) = (allyId, damageTaken, remainingHp);
    }
}
