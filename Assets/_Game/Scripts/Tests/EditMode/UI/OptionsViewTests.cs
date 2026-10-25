using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Presentation;
using Game.Presentation.Settings;
using Game.Presentation.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using AudioSettings = Game.Presentation.Settings.AudioSettings;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.UI
{
    public class OptionsViewTests
    {
        private readonly List<GameObject> _createdObjects = new();

        private string _masterKey;
        private string _sfxKey;
        private string _bgmKey;
        private string _languageKey;
        private string _abilityKey;

        private AudioSettings _audioSettings;
        private LocalizationSettings _localizationSettings;
        private PlayerPrefsAbilitySettings _abilitySettings;

        private OptionsView _optionsView;
        private Slider _masterSlider;
        private Slider _sfxSlider;
        private Slider _bgmSlider;
        private Toggle _abilityToggle;
        private Button _abilityButton;
        private TMP_Text _abilityText;
        private Dropdown _legacyDropdown;
        private Button _closeButton;

        [SetUp]
        public void SetUp()
        {
            _masterKey = $"Test.Audio.Master.{Guid.NewGuid():N}";
            _sfxKey = $"Test.Audio.Sfx.{Guid.NewGuid():N}";
            _bgmKey = $"Test.Audio.Bgm.{Guid.NewGuid():N}";
            _languageKey = $"Test.Language.{Guid.NewGuid():N}";
            _abilityKey = $"Test.Ability.{Guid.NewGuid():N}";

            _audioSettings = new AudioSettings(0.7f, 0.5f, 0.3f, _masterKey, _sfxKey, _bgmKey);
            _localizationSettings = new LocalizationSettings("pt-BR", new[] { "pt-BR", "en-US", "es" }, _languageKey);
            _abilitySettings = new PlayerPrefsAbilitySettings(_abilityKey, HeroAbilityTriggerMode.Auto);

            _optionsView = CreateComponent<OptionsView>("OptionsView");
            _masterSlider = CreateComponent<Slider>("MasterSlider");
            _sfxSlider = CreateComponent<Slider>("SfxSlider");
            _bgmSlider = CreateComponent<Slider>("BgmSlider");
            _abilityToggle = CreateComponent<Toggle>("AbilityToggle");
            _abilityButton = CreateComponent<Button>("AbilityButton");
            _abilityText = CreateComponent<TextMeshProUGUI>("AbilityText");
            _legacyDropdown = CreateComponent<Dropdown>("LanguageDropdown");
            _closeButton = CreateComponent<Button>("CloseButton");

            _optionsView.SetupControls(
                master: _masterSlider,
                sfx: _sfxSlider,
                bgm: _bgmSlider,
                abilityToggle: _abilityToggle,
                abilityBtn: _abilityButton,
                abilityTxt: _abilityText,
                legacyLangDropdown: _legacyDropdown,
                closeBtn: _closeButton
            );

            _optionsView.Initialize(_audioSettings, _localizationSettings, _abilitySettings);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private T CreateComponent<T>(string name = null) where T : Component
        {
            var go = new GameObject(name ?? typeof(T).Name);
            _createdObjects.Add(go);
            return go.AddComponent<T>();
        }

        [Test]
        public void LayerType_IsModal()
        {
            Assert.AreEqual(UILayerType.Modal, _optionsView.LayerType);
        }

        [Test]
        public void Initialize_PopulatesControlsFromSettings()
        {
            Assert.AreEqual(0.7f, _masterSlider.value, 1e-4f);
            Assert.AreEqual(0.5f, _sfxSlider.value, 1e-4f);
            Assert.AreEqual(0.3f, _bgmSlider.value, 1e-4f);

            Assert.IsTrue(_abilityToggle.isOn);
            Assert.AreEqual("Auto", _abilityText.text);

            Assert.AreEqual(0, _legacyDropdown.value);
            Assert.AreEqual("pt-BR", _legacyDropdown.options[_legacyDropdown.value].text);
        }

        [Test]
        public void VolumeSliders_UpdateAudioSettingsImmediately()
        {
            _masterSlider.value = 0.2f;
            Assert.AreEqual(0.2f, _audioSettings.MasterVolume, 1e-4f);

            _sfxSlider.value = 0.8f;
            Assert.AreEqual(0.8f, _audioSettings.SfxVolume, 1e-4f);

            _bgmSlider.value = 0.9f;
            Assert.AreEqual(0.9f, _audioSettings.BgmVolume, 1e-4f);
        }

        [Test]
        public void AbilityControls_ToggleTriggerModeImmediately()
        {
            _abilityToggle.isOn = false;
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, _abilitySettings.TriggerMode);
            Assert.AreEqual("Manual", _abilityText.text);

            _abilityButton.onClick.Invoke();
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, _abilitySettings.TriggerMode);
            Assert.IsTrue(_abilityToggle.isOn);
            Assert.AreEqual("Auto", _abilityText.text);
        }

        [Test]
        public void LanguageDropdown_UpdatesCurrentLanguageImmediately()
        {
            _legacyDropdown.value = 1;
            Assert.AreEqual("en-US", _localizationSettings.CurrentLanguage);

            _legacyDropdown.value = 2;
            Assert.AreEqual("es", _localizationSettings.CurrentLanguage);
        }

        [Test]
        public void Close_SavesAllSettingsToPersistentStorage()
        {
            _masterSlider.value = 0.4f;
            _sfxSlider.value = 0.6f;
            _bgmSlider.value = 0.1f;
            _legacyDropdown.value = 1;
            _abilityToggle.isOn = false;

            _optionsView.Open();
            Assert.IsTrue(_optionsView.IsOpen);

            _closeButton.onClick.Invoke();
            Assert.IsFalse(_optionsView.IsOpen);

            var reloadedAudio = new AudioSettings(1f, 1f, 1f, _masterKey, _sfxKey, _bgmKey);
            Assert.AreEqual(0.4f, reloadedAudio.MasterVolume, 1e-4f);
            Assert.AreEqual(0.6f, reloadedAudio.SfxVolume, 1e-4f);
            Assert.AreEqual(0.1f, reloadedAudio.BgmVolume, 1e-4f);

            var reloadedLoc = new LocalizationSettings("pt-BR", null, _languageKey);
            Assert.AreEqual("en-US", reloadedLoc.CurrentLanguage);

            var reloadedAbility = new PlayerPrefsAbilitySettings(_abilityKey, HeroAbilityTriggerMode.Auto);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, reloadedAbility.TriggerMode);
        }

        [Test]
        public void ApplySettings_DirectCall_SavesAllSettings()
        {
            _masterSlider.value = 0.9f;
            _sfxSlider.value = 0.1f;
            _bgmSlider.value = 0.5f;
            _legacyDropdown.value = 2;
            _abilityToggle.isOn = false;

            _optionsView.ApplySettings();

            var reloadedAudio = new AudioSettings(1f, 1f, 1f, _masterKey, _sfxKey, _bgmKey);
            Assert.AreEqual(0.9f, reloadedAudio.MasterVolume, 1e-4f);
            Assert.AreEqual(0.1f, reloadedAudio.SfxVolume, 1e-4f);
            Assert.AreEqual(0.5f, reloadedAudio.BgmVolume, 1e-4f);

            var reloadedLoc = new LocalizationSettings("pt-BR", null, _languageKey);
            Assert.AreEqual("es", reloadedLoc.CurrentLanguage);

            var reloadedAbility = new PlayerPrefsAbilitySettings(_abilityKey, HeroAbilityTriggerMode.Auto);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, reloadedAbility.TriggerMode);
        }

        [Test]
        public void Reopen_RefreshesViewWithExternalSettingsChanges()
        {
            _optionsView.Open();
            Assert.IsTrue(_optionsView.IsOpen);
            _optionsView.Close();
            Assert.IsFalse(_optionsView.IsOpen);

            _audioSettings.MasterVolume = 0.15f;
            _localizationSettings.CurrentLanguage = "es";
            _abilitySettings.TriggerMode = HeroAbilityTriggerMode.Manual;

            _optionsView.Open();

            Assert.AreEqual(0.15f, _masterSlider.value, 1e-4f);
            Assert.AreEqual(2, _legacyDropdown.value);
            Assert.IsFalse(_abilityToggle.isOn);
            Assert.AreEqual("Manual", _abilityText.text);
        }

        [Test]
        public void UIManager_CanRegisterAndOpenOptionsViewAsModal()
        {
            var uiManager = CreateComponent<UIManager>("UIManager");
            var modalLayer = CreateComponent<UILayer>("ModalLayer");
            modalLayer.Initialize(UILayerType.Modal);
            uiManager.RegisterLayer(modalLayer);
            uiManager.RegisterWindow(_optionsView);

            uiManager.OpenWindow<OptionsView>();
            Assert.IsTrue(_optionsView.IsOpen);
            Assert.IsTrue(uiManager.IsModalOpen());

            uiManager.CloseWindow<OptionsView>();
            Assert.IsFalse(_optionsView.IsOpen);
            Assert.IsFalse(uiManager.IsModalOpen());
        }
    }
}
