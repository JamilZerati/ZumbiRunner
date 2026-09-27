using System;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class MultiplySoldiersEffect : IPerkEffect
    {
        public int Factor;

        public MultiplySoldiersEffect()
        {
        }

        public MultiplySoldiersEffect(int factor)
        {
            Factor = factor;
        }

        public string Description => $"x{Factor}";

        public void Apply(PerkContext context)
        {
            context.Squad?.Multiply(Factor);
        }
    }
}
