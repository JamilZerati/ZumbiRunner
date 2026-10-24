using UnityEngine;

namespace Game.Presentation.Settings
{
    public class AudioSettings : IAudioSettings
    {
        public const string MasterKey = "Settings.Audio.Master";
        public const string SfxKey = "Settings.Audio.Sfx";
        public const string BgmKey = "Settings.Audio.Bgm";

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

        public AudioSettings(float defaultMaster = 1.0f, float defaultSfx = 1.0f, float defaultBgm = 1.0f)
        {
            _masterVolume = PlayerPrefs.GetFloat(MasterKey, defaultMaster);
            _sfxVolume = PlayerPrefs.GetFloat(SfxKey, defaultSfx);
            _bgmVolume = PlayerPrefs.GetFloat(BgmKey, defaultBgm);
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(MasterKey, _masterVolume);
            PlayerPrefs.SetFloat(SfxKey, _sfxVolume);
            PlayerPrefs.SetFloat(BgmKey, _bgmVolume);
            PlayerPrefs.Save();
        }
    }
}
