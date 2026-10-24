using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.Settings
{
    public class LocalizationSettings : ILocalizationSettings
    {
        public const string LanguageKey = "Settings.Language";
        private static readonly string[] DefaultLanguages = { "pt-BR", "en-US", "es" };

        private readonly List<string> _supportedLanguages;
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

        public IReadOnlyList<string> SupportedLanguages => _supportedLanguages;

        public LocalizationSettings(string defaultLanguage = "pt-BR", IEnumerable<string> supportedLanguages = null)
        {
            _supportedLanguages = supportedLanguages != null ? new List<string>(supportedLanguages) : new List<string>(DefaultLanguages);
            string saved = PlayerPrefs.GetString(LanguageKey, defaultLanguage);
            _currentLanguage = _supportedLanguages.Contains(saved) ? saved : defaultLanguage;
        }

        public void Save()
        {
            PlayerPrefs.SetString(LanguageKey, _currentLanguage);
            PlayerPrefs.Save();
        }
    }
}
