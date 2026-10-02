using System;

namespace Game.Core.Abilities.Policies
{
    public class ManualHeroAbilityTriggerPolicy : IHeroAbilityTriggerPolicy
    {
        public HeroAbilityTriggerMode Mode => HeroAbilityTriggerMode.Manual;

        public bool ShouldTrigger(in AbilityTriggerContext context)
        {
            throw new NotImplementedException();
        }
    }
}
