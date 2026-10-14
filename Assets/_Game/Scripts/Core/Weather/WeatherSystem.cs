using System;
using Game.Core.Gates;

namespace Game.Core.Weather
{
    public class WeatherSystem : IWeatherSystem
    {
        public WeatherType CurrentWeather => WeatherType.Clear;
        public float WeatherRemainingDistance => 0f;
        public bool IsWeatherActive => false;

        public void SetWeather(WeatherType type, float durationDistance, StageModifierController stageModifiers = null) => throw new NotImplementedException();
        public void ClearWeather(StageModifierController stageModifiers = null) => throw new NotImplementedException();
        public void TickDistance(float currentDistance, StageModifierController stageModifiers = null) => throw new NotImplementedException();
    }
}
