using System;
using Game.Core.Stats;

namespace Game.Core.Perks
{
    public readonly struct PerkContext
    {
        public ISquad Squad { get; }
        public int LaneIndex { get; }
        public IWeaponLoadout Loadout => throw new NotImplementedException();
        public string SourceId => throw new NotImplementedException();

        public PerkContext(ISquad squad, int laneIndex = 0, IWeaponLoadout loadout = null, string sourceId = null)
        {
            Squad = squad;
            LaneIndex = laneIndex;
        }

        public PerkContext WithSourceId(string sourceId) => throw new NotImplementedException();
    }
}
