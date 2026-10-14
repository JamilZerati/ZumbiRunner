using System;
using Game.Core.Allies;

namespace Game.Core.Perks.Effects
{
    public sealed class RecruitAllyPerkEffect : IPerkEffect
    {
        public string AllyId { get; }
        public string Description => $"Recrutar aliado {AllyId}";

        public RecruitAllyPerkEffect(string allyId)
        {
            AllyId = allyId ?? string.Empty;
        }

        public void Apply(PerkContext context)
        {
            throw new NotImplementedException("RecruitAllyPerkEffect.Apply pendente de implementação no Marco 1.");
        }
    }
}
