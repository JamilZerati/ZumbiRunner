namespace Game.Core.Events
{
    public readonly struct EnemyEngagedEvent
    {
        public readonly string ArchetypeId;
        public readonly int LaneIndex;

        public EnemyEngagedEvent(string archetypeId, int laneIndex)
        {
            ArchetypeId = archetypeId;
            LaneIndex = laneIndex;
        }
    }
}
