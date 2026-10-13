namespace Game.Core.Events
{
    public readonly struct SurvivorsRescuedEvent
    {
        public int Count { get; }
        public int LaneIndex { get; }
        public string CageId { get; }

        public SurvivorsRescuedEvent(int count, int laneIndex, string cageId = null)
        {
            Count = count;
            LaneIndex = laneIndex;
            CageId = cageId ?? string.Empty;
        }
    }
}
