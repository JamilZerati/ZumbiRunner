using System;

namespace Game.Core.Stats
{
    public readonly struct StatModifier
    {
        public StatId Stat { get; }
        public ModifierKind Kind { get; }
        public float Value { get; }
        public object Source { get; }

        public StatModifier(StatId stat, ModifierKind kind, float value, object source)
        {
            throw new NotImplementedException();
        }
    }
}
