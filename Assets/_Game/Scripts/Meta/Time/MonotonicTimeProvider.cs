using System;

namespace Game.Meta.Time
{
    public class MonotonicTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => throw new NotImplementedException();
        public long LastSeenUtcTicks => throw new NotImplementedException();
        public bool IsClockManipulated => throw new NotImplementedException();
        public void RecordCurrentTime() => throw new NotImplementedException();
    }
}
