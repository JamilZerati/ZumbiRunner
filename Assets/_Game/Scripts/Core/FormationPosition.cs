using System;

namespace Game.Core
{
    public readonly struct FormationPosition : IEquatable<FormationPosition>
    {
        public float X { get; }
        public float Z { get; }

        public FormationPosition(float x, float z)
        {
            X = x;
            Z = z;
        }

        public bool Equals(FormationPosition other)
        {
            return X.Equals(other.X) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is FormationPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Z);
        }

        public override string ToString()
        {
            return $"({X}, {Z})";
        }

        public static bool operator ==(FormationPosition left, FormationPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FormationPosition left, FormationPosition right)
        {
            return !left.Equals(right);
        }
    }
}
