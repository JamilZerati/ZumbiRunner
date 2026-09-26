using System;

namespace Game.Core
{
    public readonly struct LaneLayout
    {
        public int LaneCount { get; }

        public LaneLayout(int laneCount)
        {
            if (laneCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(laneCount), "Lane count must be at least 1.");
            }

            LaneCount = laneCount;
        }

        public bool IsValidLane(int laneIndex)
        {
            return laneIndex >= 0 && laneIndex < LaneCount;
        }
    }
}
