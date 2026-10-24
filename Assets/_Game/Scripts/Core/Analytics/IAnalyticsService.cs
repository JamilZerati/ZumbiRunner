using System.Collections.Generic;

namespace Game.Core.Analytics
{
    public interface IAnalyticsService
    {
        void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null);
        void TrackFtueStep(string stepName, int stageIndex, string status);
    }
}
