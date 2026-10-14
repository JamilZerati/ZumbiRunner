using Game.Core.Gates;

namespace Game.Core.Weather
{
    public interface IWeatherSystem
    {
        WeatherType CurrentWeather { get; }
        float WeatherRemainingDistance { get; }
        bool IsWeatherActive { get; }

        void SetWeather(WeatherType type, float durationDistance, StageModifierController stageModifiers = null);
        void ClearWeather(StageModifierController stageModifiers = null);
        void TickDistance(float currentDistance, StageModifierController stageModifiers = null);
    }
}
