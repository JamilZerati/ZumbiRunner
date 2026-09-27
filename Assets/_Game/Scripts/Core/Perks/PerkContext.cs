namespace Game.Core.Perks
{
    public readonly struct PerkContext
    {
        public ISquad Squad { get; }
        public int LaneIndex { get; }

        public PerkContext(ISquad squad, int laneIndex = 0)
        {
            Squad = squad;
            LaneIndex = laneIndex;
        }
    }
}
