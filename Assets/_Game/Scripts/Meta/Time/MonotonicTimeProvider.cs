using System;

namespace Game.Meta.Time
{
    public class MonotonicTimeProvider : ITimeProvider
    {
        private static readonly TimeSpan ClockDriftThreshold = TimeSpan.FromMinutes(10);
        private readonly Func<DateTime> _utcNowProvider;
        private long _lastSeenUtcTicks;
        private bool _isClockManipulated;

        public DateTime UtcNow => _utcNowProvider();
        public long LastSeenUtcTicks => _lastSeenUtcTicks;
        public bool IsClockManipulated => _isClockManipulated || CheckClockManipulation();

        public MonotonicTimeProvider(long initialLastSeenTicks = 0, Func<DateTime> utcNowProvider = null)
        {
            _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
            _lastSeenUtcTicks = initialLastSeenTicks;
        }

        public void RecordCurrentTime()
        {
            long currentTicks = UtcNow.Ticks;
            if (currentTicks >= _lastSeenUtcTicks)
            {
                _lastSeenUtcTicks = currentTicks;
                _isClockManipulated = false;
            }
            else if (_lastSeenUtcTicks - currentTicks > ClockDriftThreshold.Ticks)
            {
                _isClockManipulated = true;
            }
        }

        private bool CheckClockManipulation()
        {
            if (_lastSeenUtcTicks == 0) return false;
            long currentTicks = UtcNow.Ticks;
            return currentTicks < _lastSeenUtcTicks - ClockDriftThreshold.Ticks;
        }
    }
}
