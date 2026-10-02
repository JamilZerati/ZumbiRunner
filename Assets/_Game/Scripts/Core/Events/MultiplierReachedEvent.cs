namespace Game.Core.Events
{
    public readonly struct MultiplierReachedEvent
    {
        public int Multiplier { get; }
        public int SoldiersSacrificed { get; }

        public MultiplierReachedEvent(int multiplier, int soldiersSacrificed)
        {
            Multiplier = multiplier;
            SoldiersSacrificed = soldiersSacrificed;
        }
    }
}
