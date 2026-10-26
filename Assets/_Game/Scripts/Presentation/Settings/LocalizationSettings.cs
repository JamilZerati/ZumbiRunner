using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Settings
{
    public class LocalizationSettings : ILocalizationSettings
    {
        public const string LanguageKey = "Settings.Language";
        private static readonly string[] DefaultLanguages = { "pt-BR", "en-US", "es" };

        private readonly string _storageKey;
        private readonly List<string> _supportedLanguages;
        private readonly IReadOnlyList<string> _supportedLanguagesReadOnly;
        private string _currentLanguage;

        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (!string.IsNullOrEmpty(value) && _supportedLanguages.Contains(value))
                {
                    _currentLanguage = value;
                }
            }
        }

        public IReadOnlyList<string> SupportedLanguages => _supportedLanguagesReadOnly;

        public LocalizationSettings(string defaultLanguage = "pt-BR", IEnumerable<string> supportedLanguages = null, string storageKey = LanguageKey)
        {
            _storageKey = storageKey;
            _supportedLanguages = supportedLanguages != null ? new List<string>(supportedLanguages) : new List<string>(DefaultLanguages);
            if (_supportedLanguages.Count == 0)
            {
                _supportedLanguages.Add(defaultLanguage);
            }
            _supportedLanguagesReadOnly = _supportedLanguages.AsReadOnly();

            string saved = PlayerPrefs.GetString(_storageKey, defaultLanguage);
            if (_supportedLanguages.Contains(saved))
            {
                _currentLanguage = saved;
            }
            else if (_supportedLanguages.Contains(defaultLanguage))
            {
                _currentLanguage = defaultLanguage;
            }
            else
            {
                _currentLanguage = _supportedLanguages[0];
            }
        }

        public void Save()
        {
            PlayerPrefs.SetString(_storageKey, _currentLanguage);
            PlayerPrefs.Save();
        }
    }
}
