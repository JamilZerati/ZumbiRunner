using System;
using UnityEngine;
using Game.Core;
using Game.Core.Events;
using Game.Core.Weather;

namespace Game.Presentation
{
    public class WeatherVisualController : MonoBehaviour
    {
        [SerializeField] private GameObject rainEffect;
        [SerializeField] private GameObject blizzardEffect;
        [SerializeField] private GameObject heatwaveEffect;
        [SerializeField] private GameObject fogEffect;

        private IEventBus _eventBus;
        private IWeatherSystem _weatherSystem;
        private IDisposable _weatherChangedSub;

        public WeatherType CurrentActiveWeather { get; private set; } = WeatherType.Clear;

        public GameObject RainEffect => rainEffect;
        public GameObject BlizzardEffect => blizzardEffect;
        public GameObject HeatwaveEffect => heatwaveEffect;
        public GameObject FogEffect => fogEffect;

        public void Initialize(IEventBus bus = null, IWeatherSystem weatherSystem = null,
                               GameObject rain = null, GameObject blizzard = null,
                               GameObject heatwave = null, GameObject fog = null)
        {
            CleanupSubscription();

            _eventBus = bus;
            _weatherSystem = weatherSystem;

            if (rain != null) rainEffect = rain;
            if (blizzard != null) blizzardEffect = blizzard;
            if (heatwave != null) heatwaveEffect = heatwave;
            if (fog != null) fogEffect = fog;

            if (_eventBus != null)
            {
                _weatherChangedSub = _eventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            }

            if (_weatherSystem != null)
            {
                ApplyWeather(_weatherSystem.CurrentWeather);
            }
            else
            {
                ApplyWeather(WeatherType.Clear);
            }
        }

        private void OnEnable()
        {
            if (_eventBus != null && _weatherChangedSub == null)
            {
                _weatherChangedSub = _eventBus.Subscribe<WeatherChangedEvent>(OnWeatherChanged);
            }
        }

        private void OnDisable()
        {
            CleanupSubscription();
        }

        private void OnDestroy()
        {
            CleanupSubscription();
        }

        private void CleanupSubscription()
        {
            _weatherChangedSub?.Dispose();
            _weatherChangedSub = null;
        }

        private void OnWeatherChanged(WeatherChangedEvent evt)
        {
            ApplyWeather(evt.NewWeather);
        }

        public void ApplyWeather(WeatherType weather)
        {
            CurrentActiveWeather = weather;

            if (rainEffect != null)
            {
                rainEffect.SetActive(weather == WeatherType.Rain);
            }

            if (blizzardEffect != null)
            {
                blizzardEffect.SetActive(weather == WeatherType.Blizzard);
            }

            if (heatwaveEffect != null)
            {
                heatwaveEffect.SetActive(weather == WeatherType.Heatwave);
            }

            if (fogEffect != null)
            {
                fogEffect.SetActive(weather == WeatherType.Fog);
            }
        }
    }
}
