using Game.Core.Stats;

namespace Game.Core.Perks
{
    public readonly struct PerkContext
    {
        public ISquad Squad { get; }
        public int LaneIndex { get; }
        public IWeaponLoadout Loadout { get; }
        public string SourceId { get; }

        public PerkContext(ISquad squad, int laneIndex = 0, IWeaponLoadout loadout = null, string sourceId = null)
        {
            Squad = squad;
            LaneIndex = laneIndex;
            Loadout = loadout;
            SourceId = sourceId;
        }

        public PerkContext WithSourceId(string sourceId) => new PerkContext(Squad, LaneIndex, Loadout, sourceId);
    }
}
