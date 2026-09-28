using System;

namespace Game.Core.Stats
{
    public readonly struct StatModifier : IEquatable<StatModifier>
    {
        public StatId Stat { get; }
        public ModifierKind Kind { get; }
        public float Value { get; }
        public object Source { get; }

        public StatModifier(StatId stat, ModifierKind kind, float value, object source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Multiplicar por (1 + value) com value <= -1 zeraria ou inverteria o stat.
            if (kind == ModifierKind.PercentMultiply && value <= -1f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value,
                    "PercentMultiply precisa de valor maior que -1.");
            }

            Stat = stat;
            Kind = kind;
            Value = value;
            Source = source;
        }

        public bool Equals(StatModifier other)
        {
            return Stat == other.Stat
                && Kind == other.Kind
                && Value.Equals(other.Value)
                && Equals(Source, other.Source);
        }

        public override bool Equals(object obj) => obj is StatModifier other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Stat;
                hash = hash * 397 ^ (int)Kind;
                hash = hash * 397 ^ Value.GetHashCode();
                hash = hash * 397 ^ (Source?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
