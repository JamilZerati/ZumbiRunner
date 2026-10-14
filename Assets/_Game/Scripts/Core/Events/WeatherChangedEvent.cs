namespace Game.Core.Events
{
    public readonly struct WeatherChangedEvent
    {
        public Game.Core.Weather.WeatherType PreviousWeather { get; }
        public Game.Core.Weather.WeatherType NewWeather { get; }
        public float DurationDistance { get; }

        public WeatherChangedEvent(Game.Core.Weather.WeatherType previous, Game.Core.Weather.WeatherType next, float duration)
        {
            PreviousWeather = previous;
            NewWeather = next;
            DurationDistance = duration;
        }
    }
}
