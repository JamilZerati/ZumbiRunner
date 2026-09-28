using System;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class DivideSoldiersEffect : IPerkEffect
    {
        public int Divisor;

        public DivideSoldiersEffect()
        {
        }

        public DivideSoldiersEffect(int divisor)
        {
            Divisor = divisor;
        }

        public string Description => $"÷{Divisor}";

        public void Apply(PerkContext context)
        {
            if (Divisor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Divisor), "Divisor must be greater than zero.");
            }

            context.Squad?.Divide(Divisor);
        }
    }
}
