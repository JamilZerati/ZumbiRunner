namespace Game.Core.Events
{
    public readonly struct EnemyEscapedEvent
    {
        public string ArchetypeId { get; }
        public int Lane { get; }
        public float ZPosition { get; }

        public EnemyEscapedEvent(string archetypeId, int lane, float zPosition)
        {
            ArchetypeId = archetypeId;
            Lane = lane;
            ZPosition = zPosition;
        }
    }
}
