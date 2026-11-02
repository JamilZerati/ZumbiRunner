using System;
using Game.Meta.Time;

namespace Game.Tests.EditMode.Meta.Doubles
{
    public class FakeTimeProvider : ITimeProvider
    {
        private DateTime _utcNow;
        private long _lastSeenUtcTicks;
        private bool _isClockManipulated;

        public DateTime UtcNow => _utcNow;
        public long LastSeenUtcTicks => _lastSeenUtcTicks;
        public bool IsClockManipulated => _isClockManipulated;

        public FakeTimeProvider(DateTime? initialUtc = null)
        {
            _utcNow = initialUtc ?? new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
            _lastSeenUtcTicks = _utcNow.Ticks;
            _isClockManipulated = false;
        }

        public void SetUtcNow(DateTime time)
        {
            if (time.Ticks < _lastSeenUtcTicks - TimeSpan.FromMinutes(10).Ticks)
            {
                _isClockManipulated = true;
            }
            else if (time.Ticks >= _lastSeenUtcTicks)
            {
                _lastSeenUtcTicks = time.Ticks;
                _isClockManipulated = false;
            }

            _utcNow = time;
        }

        public void Advance(TimeSpan delta)
        {
            _utcNow = _utcNow.Add(delta);
            if (_utcNow.Ticks >= _lastSeenUtcTicks)
            {
                _lastSeenUtcTicks = _utcNow.Ticks;
                _isClockManipulated = false;
            }
            else if (_lastSeenUtcTicks - _utcNow.Ticks > TimeSpan.FromMinutes(10).Ticks)
            {
                _isClockManipulated = true;
            }
        }

        public void SetManipulated(bool manipulated)
        {
            _isClockManipulated = manipulated;
        }

        public void RecordCurrentTime()
        {
            if (_utcNow.Ticks >= _lastSeenUtcTicks)
            {
                _lastSeenUtcTicks = _utcNow.Ticks;
                _isClockManipulated = false;
            }
            else if (_lastSeenUtcTicks - _utcNow.Ticks > TimeSpan.FromMinutes(10).Ticks)
            {
                _isClockManipulated = true;
            }
        }
    }
}
