namespace Game.Core.Events
{
    public readonly struct SurvivorsLostEvent
    {
        public int Count { get; }
        public int LaneIndex { get; }
        public string CageId { get; }

        public SurvivorsLostEvent(int count, int laneIndex, string cageId = null)
        {
            Count = count;
            LaneIndex = laneIndex;
            CageId = cageId ?? string.Empty;
        }
    }
}
