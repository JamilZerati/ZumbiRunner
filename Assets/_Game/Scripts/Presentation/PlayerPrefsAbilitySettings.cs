using System;
using Game.Core.Abilities;
using UnityEngine;

namespace Game.Presentation
{
    public class PlayerPrefsAbilitySettings : IAbilitySettings
    {
        public const string DefaultPrefsKey = "Ability.TriggerMode";

        private readonly string _prefsKey;
        private readonly HeroAbilityTriggerMode _defaultMode;

        public PlayerPrefsAbilitySettings(string prefsKey = DefaultPrefsKey, HeroAbilityTriggerMode defaultMode = HeroAbilityTriggerMode.Auto)
        {
            _prefsKey = string.IsNullOrEmpty(prefsKey) ? DefaultPrefsKey : prefsKey;
            _defaultMode = defaultMode;
        }

        public HeroAbilityTriggerMode TriggerMode
        {
            get => LoadTriggerMode();
            set => SaveTriggerMode(value);
        }

        public HeroAbilityTriggerMode LoadTriggerMode()
        {
            int raw = PlayerPrefs.GetInt(_prefsKey, (int)_defaultMode);
            if (Enum.IsDefined(typeof(HeroAbilityTriggerMode), raw))
            {
                return (HeroAbilityTriggerMode)raw;
            }

            return _defaultMode;
        }

        public void SaveTriggerMode(HeroAbilityTriggerMode mode)
        {
            PlayerPrefs.SetInt(_prefsKey, (int)mode);
            PlayerPrefs.Save();
        }
    }
}
