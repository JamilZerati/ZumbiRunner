using System;
using Game.Core.Events;
using Game.Core.Gates;

namespace Game.Core.Weather
{
    public sealed class WeatherSystem : IWeatherSystem
    {
        private readonly IEventBus _eventBus;
        private WeatherType _currentWeather = WeatherType.Clear;
        private float _startDistance;
        private float _endDistance;
        private float _currentDistance;
        private WeatherStageModifier _activeModifier;

        public WeatherType CurrentWeather => IsWeatherActive ? _currentWeather : WeatherType.Clear;
        public float WeatherRemainingDistance => IsWeatherActive ? Math.Max(0f, _endDistance - _currentDistance) : 0f;
        public bool IsWeatherActive => _currentWeather != WeatherType.Clear && _currentDistance < _endDistance;

        public WeatherSystem(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public void SetWeather(WeatherType type, float durationDistance, StageModifierController stageModifiers = null)
        {
            if (durationDistance <= 0f || type == WeatherType.Clear)
            {
                ClearWeather(stageModifiers);
                return;
            }

            if (stageModifiers != null)
            {
                if (_activeModifier != null)
                {
                    stageModifiers.RemoveBySourceId(_activeModifier.SourceId);
                }

                _currentDistance = Math.Max(_currentDistance, stageModifiers.CurrentDistance);
            }

            var previousWeather = CurrentWeather;
            _startDistance = _currentDistance;
            _endDistance = _startDistance + durationDistance;
            _currentWeather = type;
            _activeModifier = new WeatherStageModifier(type);

            stageModifiers?.Activate(_activeModifier, _startDistance, durationDistance);

            if (previousWeather != type)
            {
                _eventBus?.Publish(new WeatherChangedEvent(previousWeather, type, durationDistance));
            }
        }

        public void ClearWeather(StageModifierController stageModifiers = null)
        {
            if (_currentWeather == WeatherType.Clear && _activeModifier == null)
            {
                return;
            }

            var previousWeather = _currentWeather;

            if (stageModifiers != null && _activeModifier != null)
            {
                stageModifiers.RemoveBySourceId(_activeModifier.SourceId);
            }

            _activeModifier = null;
            _currentWeather = WeatherType.Clear;
            _endDistance = 0f;

            if (previousWeather != WeatherType.Clear)
            {
                _eventBus?.Publish(new WeatherChangedEvent(previousWeather, WeatherType.Clear, 0f));
            }
        }

        public void TickDistance(float currentDistance, StageModifierController stageModifiers = null)
        {
            _currentDistance = currentDistance;
            stageModifiers?.TickDistance(currentDistance);

            if (_currentWeather != WeatherType.Clear && currentDistance >= _endDistance)
            {
                ClearWeather(stageModifiers);
            }
        }
    }
}
