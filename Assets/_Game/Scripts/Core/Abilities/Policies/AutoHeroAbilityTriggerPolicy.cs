using System;

namespace Game.Core.Abilities.Policies
{
    public class AutoHeroAbilityTriggerPolicy : IHeroAbilityTriggerPolicy
    {
        public HeroAbilityTriggerMode Mode => HeroAbilityTriggerMode.Auto;

        public bool ShouldTrigger(in AbilityTriggerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
