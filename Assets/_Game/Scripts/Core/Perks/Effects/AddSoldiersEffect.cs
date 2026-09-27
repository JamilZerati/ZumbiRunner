using System;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class AddSoldiersEffect : IPerkEffect
    {
        public int Amount;

        public AddSoldiersEffect()
        {
        }

        public AddSoldiersEffect(int amount)
        {
            Amount = amount;
        }

        public string Description => Amount >= 0 ? $"+{Amount}" : $"{Amount}";

        public void Apply(PerkContext context)
        {
            if (context.Squad == null)
            {
                return;
            }

            if (Amount >= 0)
            {
                context.Squad.Add(Amount);
            }
            else
            {
                context.Squad.Remove(-Amount);
            }
        }
    }
}
