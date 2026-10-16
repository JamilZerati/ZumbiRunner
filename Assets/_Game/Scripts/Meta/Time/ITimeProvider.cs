using System;

namespace Game.Meta.Time
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
        long LastSeenUtcTicks { get; }
        bool IsClockManipulated { get; }
        void RecordCurrentTime();
    }
}
