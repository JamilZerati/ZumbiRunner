using System.Collections.Generic;
using Game.Core.Abilities;
using Game.Presentation.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    public class OptionsView : UIWindow
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Toggle abilityModeToggle;
        [SerializeField] private Button abilityModeButton;
        [SerializeField] private TMP_Text abilityModeText;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [SerializeField] private Dropdown legacyLanguageDropdown;
        [SerializeField] private Button closeButton;

        private IAudioSettings _audioSettings;
        private ILocalizationSettings _localizationSettings;
        private IAbilitySettings _abilitySettings;
        private bool _isUpdatingUi;

        public override UILayerType LayerType => UILayerType.Modal;

        public Slider MasterVolumeSlider => masterVolumeSlider;
        public Slider SfxVolumeSlider => sfxVolumeSlider;
        public Slider BgmVolumeSlider => bgmVolumeSlider;
        public Toggle AbilityModeToggle => abilityModeToggle;
        public Button AbilityModeButton => abilityModeButton;
        public TMP_Text AbilityModeText => abilityModeText;
        public TMP_Dropdown LanguageDropdown => languageDropdown;
        public Dropdown LegacyLanguageDropdown => legacyLanguageDropdown;
        public Button CloseButton => closeButton;

        public IAudioSettings AudioSettings => _audioSettings;
        public ILocalizationSettings LocalizationSettings => _localizationSettings;
        public IAbilitySettings AbilitySettings => _abilitySettings;

        protected override void Awake()
        {
            base.Awake();
            WireListeners();
        }

        protected override void OnDestroy()
        {
            UnwireListeners();
            base.OnDestroy();
        }

        public void SetupControls(
            Slider master = null,
            Slider sfx = null,
            Slider bgm = null,
            Toggle abilityToggle = null,
            Button abilityBtn = null,
            TMP_Text abilityTxt = null,
            TMP_Dropdown langDropdown = null,
            Dropdown legacyLangDropdown = null,
            Button closeBtn = null)
        {
            UnwireListeners();

            if (master != null) masterVolumeSlider = master;
            if (sfx != null) sfxVolumeSlider = sfx;
            if (bgm != null) bgmVolumeSlider = bgm;
            if (abilityToggle != null) abilityModeToggle = abilityToggle;
            if (abilityBtn != null) abilityModeButton = abilityBtn;
            if (abilityTxt != null) abilityModeText = abilityTxt;
            if (langDropdown != null) languageDropdown = langDropdown;
            if (legacyLangDropdown != null) legacyLanguageDropdown = legacyLangDropdown;
            if (closeBtn != null) closeButton = closeBtn;

            WireListeners();
            RefreshView();
        }

        public void Initialize(
            IAudioSettings audioSettings,
            ILocalizationSettings localizationSettings,
            IAbilitySettings abilitySettings)
        {
            _audioSettings = audioSettings;
            _localizationSettings = localizationSettings;
            _abilitySettings = abilitySettings;

            RefreshView();
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            RefreshView();
        }

        protected override void OnClose()
        {
            base.OnClose();
            ApplySettings();
        }

        public void RefreshView()
        {
            _isUpdatingUi = true;

            if (_audioSettings != null)
            {
                if (masterVolumeSlider != null) masterVolumeSlider.value = _audioSettings.MasterVolume;
                if (sfxVolumeSlider != null) sfxVolumeSlider.value = _audioSettings.SfxVolume;
                if (bgmVolumeSlider != null) bgmVolumeSlider.value = _audioSettings.BgmVolume;
            }

            if (_abilitySettings != null)
            {
                if (abilityModeToggle != null)
                {
                    abilityModeToggle.isOn = _abilitySettings.TriggerMode == HeroAbilityTriggerMode.Auto;
                }
                UpdateAbilityModeText();
            }

            if (_localizationSettings != null)
            {
                RefreshLanguageDropdowns();
            }

            _isUpdatingUi = false;
        }

        public void ApplySettings()
        {
            if (_audioSettings != null)
            {
                if (masterVolumeSlider != null)
                {
                    _audioSettings.MasterVolume = masterVolumeSlider.value;
                }
                if (sfxVolumeSlider != null)
                {
                    _audioSettings.SfxVolume = sfxVolumeSlider.value;
                }
                if (bgmVolumeSlider != null)
                {
                    _audioSettings.BgmVolume = bgmVolumeSlider.value;
                }
                _audioSettings.Save();
            }

            if (_localizationSettings != null)
            {
                var supported = _localizationSettings.SupportedLanguages;
                if (languageDropdown != null && languageDropdown.value >= 0 && languageDropdown.value < supported.Count)
                {
                    _localizationSettings.CurrentLanguage = supported[languageDropdown.value];
                }
                else if (legacyLanguageDropdown != null && legacyLanguageDropdown.value >= 0 && legacyLanguageDropdown.value < supported.Count)
                {
                    _localizationSettings.CurrentLanguage = supported[legacyLanguageDropdown.value];
                }
                _localizationSettings.Save();
            }

            if (_abilitySettings != null)
            {
                if (abilityModeToggle != null)
                {
                    _abilitySettings.TriggerMode = abilityModeToggle.isOn
                        ? HeroAbilityTriggerMode.Auto
                        : HeroAbilityTriggerMode.Manual;
                }
                _abilitySettings.SaveTriggerMode(_abilitySettings.TriggerMode);
            }
        }

        private void WireListeners()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            if (abilityModeToggle != null) abilityModeToggle.onValueChanged.AddListener(OnAbilityToggleChanged);
            if (abilityModeButton != null) abilityModeButton.onClick.AddListener(OnAbilityButtonClicked);
            if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
            if (legacyLanguageDropdown != null) legacyLanguageDropdown.onValueChanged.AddListener(OnLegacyLanguageDropdownChanged);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void UnwireListeners()
        {
            if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (bgmVolumeSlider != null) bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            if (abilityModeToggle != null) abilityModeToggle.onValueChanged.RemoveListener(OnAbilityToggleChanged);
            if (abilityModeButton != null) abilityModeButton.onClick.RemoveListener(OnAbilityButtonClicked);
            if (languageDropdown != null) languageDropdown.onValueChanged.RemoveListener(OnLanguageDropdownChanged);
            if (legacyLanguageDropdown != null) legacyLanguageDropdown.onValueChanged.RemoveListener(OnLegacyLanguageDropdownChanged);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (_isUpdatingUi) return;
            if (_audioSettings != null) _audioSettings.MasterVolume = value;
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_isUpdatingUi) return;
            if (_audioSettings != null) _audioSettings.SfxVolume = value;
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (_isUpdatingUi) return;
            if (_audioSettings != null) _audioSettings.BgmVolume = value;
        }

        private void OnAbilityToggleChanged(bool isOn)
        {
            if (_isUpdatingUi) return;
            if (_abilitySettings != null)
            {
                _abilitySettings.TriggerMode = isOn ? HeroAbilityTriggerMode.Auto : HeroAbilityTriggerMode.Manual;
                UpdateAbilityModeText();
            }
        }

        private void OnAbilityButtonClicked()
        {
            if (_abilitySettings == null) return;
            var nextMode = _abilitySettings.TriggerMode == HeroAbilityTriggerMode.Auto
                ? HeroAbilityTriggerMode.Manual
                : HeroAbilityTriggerMode.Auto;
            _abilitySettings.TriggerMode = nextMode;
            if (abilityModeToggle != null)
            {
                _isUpdatingUi = true;
                abilityModeToggle.isOn = nextMode == HeroAbilityTriggerMode.Auto;
                _isUpdatingUi = false;
            }
            UpdateAbilityModeText();
        }

        private void OnLanguageDropdownChanged(int index)
        {
            if (_isUpdatingUi) return;
            if (_localizationSettings != null && index >= 0 && index < _localizationSettings.SupportedLanguages.Count)
            {
                _localizationSettings.CurrentLanguage = _localizationSettings.SupportedLanguages[index];
            }
        }

        private void OnLegacyLanguageDropdownChanged(int index)
        {
            if (_isUpdatingUi) return;
            if (_localizationSettings != null && index >= 0 && index < _localizationSettings.SupportedLanguages.Count)
            {
                _localizationSettings.CurrentLanguage = _localizationSettings.SupportedLanguages[index];
            }
        }

        private void OnCloseClicked()
        {
            Close();
        }

        private void UpdateAbilityModeText()
        {
            if (abilityModeText != null && _abilitySettings != null)
            {
                abilityModeText.text = _abilitySettings.TriggerMode == HeroAbilityTriggerMode.Auto ? "Auto" : "Manual";
            }
        }

        private void RefreshLanguageDropdowns()
        {
            var supported = _localizationSettings.SupportedLanguages;
            if (supported == null || supported.Count == 0) return;

            int selectedIndex = 0;
            for (int i = 0; i < supported.Count; i++)
            {
                if (supported[i] == _localizationSettings.CurrentLanguage)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (languageDropdown != null)
            {
                languageDropdown.ClearOptions();
                var options = new List<TMP_Dropdown.OptionData>();
                for (int i = 0; i < supported.Count; i++)
                {
                    options.Add(new TMP_Dropdown.OptionData(supported[i]));
                }
                languageDropdown.AddOptions(options);
                languageDropdown.value = selectedIndex;
            }

            if (legacyLanguageDropdown != null)
            {
                legacyLanguageDropdown.ClearOptions();
                var options = new List<Dropdown.OptionData>();
                for (int i = 0; i < supported.Count; i++)
                {
                    options.Add(new Dropdown.OptionData(supported[i]));
                }
                legacyLanguageDropdown.AddOptions(options);
                legacyLanguageDropdown.value = selectedIndex;
            }
        }
    }
}
