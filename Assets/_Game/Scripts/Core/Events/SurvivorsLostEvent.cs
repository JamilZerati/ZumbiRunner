namespace Game.Core.Events
{
    public readonly struct SurvivorsLostEvent
    {
        private readonly string _lostEnemyArchetypeId;

        public int Count { get; }
        public int LaneIndex { get; }
        public string CageId { get; }
        public string LostEnemyArchetypeId => string.IsNullOrEmpty(_lostEnemyArchetypeId) ? "walker" : _lostEnemyArchetypeId;

        public SurvivorsLostEvent(int count, int laneIndex, string cageId = null, string lostEnemyArchetypeId = "walker")
        {
            Count = count;
            LaneIndex = laneIndex;
            CageId = cageId ?? string.Empty;
            _lostEnemyArchetypeId = string.IsNullOrEmpty(lostEnemyArchetypeId) ? "walker" : lostEnemyArchetypeId;
        }
    }
}
