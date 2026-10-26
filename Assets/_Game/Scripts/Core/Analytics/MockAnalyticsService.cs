using System;
using System.Collections.Generic;

namespace Game.Core.Analytics
{
    public class MockAnalyticsService : IAnalyticsService
    {
        private readonly List<AnalyticsTrackedEvent> _events = new();

        public IReadOnlyList<AnalyticsTrackedEvent> Events => _events;

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            _events.Add(new AnalyticsTrackedEvent(eventName, parameters));
        }

        public void TrackFtueStep(string stepName, int stageIndex, string status)
        {
            var dict = new Dictionary<string, object>
            {
                ["step_name"] = stepName ?? string.Empty,
                ["stage_index"] = stageIndex,
                ["status"] = status ?? string.Empty
            };
            TrackEvent(AnalyticsEvents.FtueStep, dict);
        }

        public bool HasEvent(string eventName)
        {
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].EventName == eventName)
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyList<AnalyticsTrackedEvent> GetEvents(string eventName)
        {
            var matching = new List<AnalyticsTrackedEvent>();
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].EventName == eventName)
                {
                    matching.Add(_events[i]);
                }
            }

            return matching;
        }

        public void Clear()
        {
            _events.Clear();
        }
    }

    public class AnalyticsTrackedEvent
    {
        public string EventName { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }

        public AnalyticsTrackedEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            EventName = eventName ?? string.Empty;
            Parameters = parameters != null
                ? new Dictionary<string, object>(parameters)
                : new Dictionary<string, object>();
        }

        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }

        public object GetParameter(string key)
        {
            return Parameters.TryGetValue(key, out var value) ? value : null;
        }

        public T GetParameter<T>(string key)
        {
            if (Parameters.TryGetValue(key, out var raw) && raw is T typed)
            {
                return typed;
            }

            return default;
        }
    }
}
