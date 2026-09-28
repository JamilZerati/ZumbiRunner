namespace Game.Core.Events
{
    public readonly struct GateTriggeredEvent
    {
        public int LaneIndex { get; }
        public string PerkId { get; }

        public GateTriggeredEvent(int laneIndex, string perkId)
        {
            LaneIndex = laneIndex;
            PerkId = perkId;
        }
    }
}
