namespace Game.Core.Events
{
    public readonly struct LaneChangedEvent
    {
        public int PreviousLane { get; }
        public int NewLane { get; }

        public LaneChangedEvent(int previousLane, int newLane)
        {
            PreviousLane = previousLane;
            NewLane = newLane;
        }
    }
}
