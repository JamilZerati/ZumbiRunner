using Game.Core.Abilities;
using Game.Presentation;
using Game.Presentation.Settings;
using NUnit.Framework;

namespace Game.Tests.EditMode.UI
{
    public class OptionsSettingsTests
    {
        [Test]
        public void CT_4_AudioSettings_ClampsVolumesBetweenZeroAndOne()
        {
            var settings = new AudioSettings();

            settings.MasterVolume = 0.5f;
            Assert.AreEqual(0.5f, settings.MasterVolume, 1e-4f);

            settings.MasterVolume = 1.5f;
            Assert.AreEqual(1.0f, settings.MasterVolume, 1e-4f);

            settings.MasterVolume = -0.2f;
            Assert.AreEqual(0.0f, settings.MasterVolume, 1e-4f);

            settings.SfxVolume = 2.0f;
            Assert.AreEqual(1.0f, settings.SfxVolume, 1e-4f);

            settings.BgmVolume = -1.0f;
            Assert.AreEqual(0.0f, settings.BgmVolume, 1e-4f);
        }

        [Test]
        public void CT_4_LocalizationSettings_ValidatesSupportedLanguages()
        {
            var settings = new LocalizationSettings("pt-BR");
            CollectionAssert.Contains(settings.SupportedLanguages, "pt-BR");
            CollectionAssert.Contains(settings.SupportedLanguages, "en-US");
            CollectionAssert.Contains(settings.SupportedLanguages, "es");

            settings.CurrentLanguage = "en-US";
            Assert.AreEqual("en-US", settings.CurrentLanguage);

            settings.CurrentLanguage = "invalid_lang";
            Assert.AreEqual("en-US", settings.CurrentLanguage);
        }

        [Test]
        public void CT_4_AbilitySettings_PersistsAutoAndManualTriggerModes()
        {
            string testKey = "Test.Ability.Mode." + System.Guid.NewGuid().ToString("N");
            var settings = new PlayerPrefsAbilitySettings(testKey, HeroAbilityTriggerMode.Auto);

            Assert.AreEqual(HeroAbilityTriggerMode.Auto, settings.TriggerMode);

            settings.TriggerMode = HeroAbilityTriggerMode.Manual;
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, settings.TriggerMode);

            var reloaded = new PlayerPrefsAbilitySettings(testKey, HeroAbilityTriggerMode.Auto);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, reloaded.TriggerMode);
        }
    }
}
