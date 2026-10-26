namespace Game.Presentation.Settings
{
    public interface IAudioSettings
    {
        float MasterVolume { get; set; }
        float SfxVolume { get; set; }
        float BgmVolume { get; set; }
        void Save();
    }
}
