namespace Game.Core.Events
{
    public readonly struct EnemyDisengagedEvent
    {
        public readonly string ArchetypeId;
        public readonly bool WasKilled;

        public EnemyDisengagedEvent(string archetypeId, bool wasKilled)
        {
            ArchetypeId = archetypeId;
            WasKilled = wasKilled;
        }
    }
}
