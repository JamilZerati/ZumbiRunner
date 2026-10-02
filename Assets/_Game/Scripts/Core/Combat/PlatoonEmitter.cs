using System;

namespace Game.Core
{
    public readonly struct PlatoonEmitter : IEquatable<PlatoonEmitter>
    {
        public readonly int Index;
        public readonly int SoldierCount;
        public readonly FormationPosition Position;

        public PlatoonEmitter(int index, int soldierCount, FormationPosition position)
        {
            Index = index;
            SoldierCount = soldierCount;
            Position = position;
        }

        public bool Equals(PlatoonEmitter other) =>
            Index == other.Index && SoldierCount == other.SoldierCount && Position.Equals(other.Position);

        public override bool Equals(object obj) => obj is PlatoonEmitter other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Index, SoldierCount, Position);
    }
}
