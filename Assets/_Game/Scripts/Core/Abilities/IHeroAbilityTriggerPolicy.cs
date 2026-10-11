namespace Game.Core.Abilities
{
    public interface IHeroAbilityTriggerPolicy
    {
        HeroAbilityTriggerMode Mode { get; }
        bool ShouldTrigger(in AbilityTriggerContext context);
    }
}
