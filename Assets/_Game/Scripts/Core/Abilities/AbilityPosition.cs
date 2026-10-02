using System;

namespace Game.Core.Abilities
{
    public readonly struct AbilityPosition : IEquatable<AbilityPosition>
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public AbilityPosition(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public bool Equals(AbilityPosition other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is AbilityPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public static bool operator ==(AbilityPosition left, AbilityPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AbilityPosition left, AbilityPosition right)
        {
            return !left.Equals(right);
        }
    }
}
