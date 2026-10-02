using Game.Core.State;

namespace Game.Core.Events
{
    public readonly struct RunStartedEvent
    {
        public string LevelId { get; }
        public RunConfig Config { get; }

        public RunStartedEvent(string levelId, RunConfig config)
        {
            LevelId = levelId;
            Config = config;
        }
    }
}
