using System;

namespace Game.Core.Abilities.Policies
{
    public class ManualHeroAbilityTriggerPolicy : IHeroAbilityTriggerPolicy
    {
        public HeroAbilityTriggerMode Mode => HeroAbilityTriggerMode.Manual;

        public bool ShouldTrigger(in AbilityTriggerContext context)
        {
            return context.CurrentCharges > 0 && context.ManualTriggerRequested;
        }
    }
}
