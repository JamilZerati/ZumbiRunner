namespace Game.Core.Events
{
    public readonly struct EnemyConsumedEvent
    {
        public readonly string ArchetypeId;
        public readonly int SoldiersLost;

        public EnemyConsumedEvent(string archetypeId, int soldiersLost)
        {
            ArchetypeId = archetypeId;
            SoldiersLost = soldiersLost;
        }
    }
}
