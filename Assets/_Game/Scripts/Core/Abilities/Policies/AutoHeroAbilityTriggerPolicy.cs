using System;

namespace Game.Core.Abilities.Policies
{
    public class AutoHeroAbilityTriggerPolicy : IHeroAbilityTriggerPolicy
    {
        public HeroAbilityTriggerMode Mode => HeroAbilityTriggerMode.Auto;

        public bool ShouldTrigger(in AbilityTriggerContext context)
        {
            return context.CurrentCharges > 0 && context.HasTargetsInLane;
        }
    }
}
