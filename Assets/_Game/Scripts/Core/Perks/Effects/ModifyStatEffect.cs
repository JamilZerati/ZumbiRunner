using System;
using Game.Core.Stats;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class ModifyStatEffect : IPerkEffect
    {
        public StatId Stat;
        public ModifierKind Kind;
        public float Value;

        public ModifyStatEffect()
        {
        }

        public ModifyStatEffect(StatId stat, ModifierKind kind, float value)
        {
            Stat = stat;
            Kind = kind;
            Value = value;
        }

        public string Description => throw new NotImplementedException();

        public void Apply(PerkContext context) => throw new NotImplementedException();
    }
}
