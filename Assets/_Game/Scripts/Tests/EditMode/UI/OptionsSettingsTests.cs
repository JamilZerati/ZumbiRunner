using System;
using Game.Core.Abilities;
using Game.Presentation;
using Game.Presentation.Settings;
using NUnit.Framework;
using UnityEngine;
using AudioSettings = Game.Presentation.Settings.AudioSettings;

namespace Game.Tests.EditMode.UI
{
    public class OptionsSettingsTests
    {
        [Test]
        public void CT_4_AudioSettings_ClampsVolumesBetweenZeroAndOne_AndPersistsAcrossInstances()
        {
            string masterKey = $"Test.Audio.Master.{Guid.NewGuid():N}";
            string sfxKey = $"Test.Audio.Sfx.{Guid.NewGuid():N}";
            string bgmKey = $"Test.Audio.Bgm.{Guid.NewGuid():N}";

            PlayerPrefs.SetFloat(masterKey, 2.5f);
            PlayerPrefs.SetFloat(sfxKey, -0.7f);
            PlayerPrefs.SetFloat(bgmKey, 1.2f);

            var settings = new AudioSettings(1.0f, 1.0f, 1.0f, masterKey, sfxKey, bgmKey);
            Assert.AreEqual(1.0f, settings.MasterVolume, 1e-4f);
            Assert.AreEqual(0.0f, settings.SfxVolume, 1e-4f);
            Assert.AreEqual(1.0f, settings.BgmVolume, 1e-4f);

            settings.MasterVolume = 0.5f;
            settings.SfxVolume = 0.8f;
            settings.BgmVolume = 0.2f;
            settings.Save();

            var reloaded = new AudioSettings(1.0f, 1.0f, 1.0f, masterKey, sfxKey, bgmKey);
            Assert.AreEqual(0.5f, reloaded.MasterVolume, 1e-4f);
            Assert.AreEqual(0.8f, reloaded.SfxVolume, 1e-4f);
            Assert.AreEqual(0.2f, reloaded.BgmVolume, 1e-4f);
        }

        [Test]
        public void CT_4_LocalizationSettings_ValidatesSupportedLanguages_AndGuaranteesInvariantAndPersistence()
        {
            var fallbackSettings = new LocalizationSettings("pt-BR", new[] { "ja", "ko" });
            Assert.AreEqual("ja", fallbackSettings.CurrentLanguage);
            CollectionAssert.Contains(fallbackSettings.SupportedLanguages, fallbackSettings.CurrentLanguage);

            string storageKey = $"Test.Language.{Guid.NewGuid():N}";
            var settings = new LocalizationSettings("pt-BR", null, storageKey);
            CollectionAssert.Contains(settings.SupportedLanguages, "pt-BR");
            CollectionAssert.Contains(settings.SupportedLanguages, "en-US");
            CollectionAssert.Contains(settings.SupportedLanguages, "es");

            settings.CurrentLanguage = "en-US";
            Assert.AreEqual("en-US", settings.CurrentLanguage);

            settings.CurrentLanguage = "invalid_lang";
            Assert.AreEqual("en-US", settings.CurrentLanguage);

            settings.Save();

            var reloaded = new LocalizationSettings("pt-BR", null, storageKey);
            Assert.AreEqual("en-US", reloaded.CurrentLanguage);
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
