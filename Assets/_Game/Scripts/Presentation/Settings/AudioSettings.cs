using UnityEngine;

namespace Game.Presentation.Settings
{
    public class AudioSettings : IAudioSettings
    {
        public const string MasterKey = "Settings.Audio.Master";
        public const string SfxKey = "Settings.Audio.Sfx";
        public const string BgmKey = "Settings.Audio.Bgm";

        private readonly string _masterKey;
        private readonly string _sfxKey;
        private readonly string _bgmKey;

        private float _masterVolume;
        private float _sfxVolume;
        private float _bgmVolume;

        public float MasterVolume
        {
            get => _masterVolume;
            set => _masterVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set => _bgmVolume = Mathf.Clamp01(value);
        }

        public AudioSettings(
            float defaultMaster = 1.0f,
            float defaultSfx = 1.0f,
            float defaultBgm = 1.0f,
            string masterKey = MasterKey,
            string sfxKey = SfxKey,
            string bgmKey = BgmKey)
        {
            _masterKey = masterKey;
            _sfxKey = sfxKey;
            _bgmKey = bgmKey;

            MasterVolume = PlayerPrefs.GetFloat(_masterKey, defaultMaster);
            SfxVolume = PlayerPrefs.GetFloat(_sfxKey, defaultSfx);
            BgmVolume = PlayerPrefs.GetFloat(_bgmKey, defaultBgm);
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(_masterKey, _masterVolume);
            PlayerPrefs.SetFloat(_sfxKey, _sfxVolume);
            PlayerPrefs.SetFloat(_bgmKey, _bgmVolume);
            PlayerPrefs.Save();
        }
    }
}
