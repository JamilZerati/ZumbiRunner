using System.Collections.Generic;

namespace Game.Core.Analytics
{
    public class MockAnalyticsService : IAnalyticsService
    {
        public readonly List<AnalyticsTrackedEvent> Events = new();

        public void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            Events.Add(new AnalyticsTrackedEvent(eventName, parameters));
        }

        public void TrackFtueStep(string stepName, int stageIndex, string status)
        {
            var dict = new Dictionary<string, object>
            {
                ["step_name"] = stepName,
                ["stage_index"] = stageIndex,
                ["status"] = status
            };
            TrackEvent(AnalyticsEvents.FtueStep, dict);
        }
    }

    public class AnalyticsTrackedEvent
    {
        public string EventName { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }

        public AnalyticsTrackedEvent(string eventName, IReadOnlyDictionary<string, object> parameters)
        {
            EventName = eventName ?? string.Empty;
            Parameters = parameters ?? new Dictionary<string, object>();
        }
    }
}
