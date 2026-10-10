using Game.Core.Abilities;

namespace Game.Presentation
{
    public interface IAbilitySettings
    {
        HeroAbilityTriggerMode TriggerMode { get; set; }
        HeroAbilityTriggerMode LoadTriggerMode();
        void SaveTriggerMode(HeroAbilityTriggerMode mode);
    }
}
