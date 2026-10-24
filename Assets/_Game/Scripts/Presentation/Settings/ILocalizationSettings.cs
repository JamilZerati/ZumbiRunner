using System.Collections.Generic;

namespace Game.Presentation.Settings
{
    public interface ILocalizationSettings
    {
        string CurrentLanguage { get; set; }
        IReadOnlyList<string> SupportedLanguages { get; }
        void Save();
    }
}
