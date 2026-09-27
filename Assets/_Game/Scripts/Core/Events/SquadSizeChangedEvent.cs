using System;

namespace Game.Core.Events
{
    public readonly struct SquadSizeChangedEvent : IEquatable<SquadSizeChangedEvent>
    {
        public int PreviousCount { get; }
        public int NewCount { get; }

        public SquadSizeChangedEvent(int previousCount, int newCount)
        {
            PreviousCount = previousCount;
            NewCount = newCount;
        }

        public bool Equals(SquadSizeChangedEvent other)
        {
            return PreviousCount == other.PreviousCount && NewCount == other.NewCount;
        }

        public override bool Equals(object obj)
        {
            return obj is SquadSizeChangedEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PreviousCount, NewCount);
        }

        public override string ToString()
        {
            return $"SquadSizeChangedEvent({PreviousCount} -> {NewCount})";
        }

        public static bool operator ==(SquadSizeChangedEvent left, SquadSizeChangedEvent right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SquadSizeChangedEvent left, SquadSizeChangedEvent right)
        {
            return !left.Equals(right);
        }
    }
}
