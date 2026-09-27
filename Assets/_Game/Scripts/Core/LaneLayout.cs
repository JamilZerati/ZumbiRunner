using System;

namespace Game.Core
{
    public readonly struct LaneLayout
    {
        public const float DefaultLaneWidth = 2.0f;

        public int LaneCount { get; }
        public float LaneWidth { get; }

        public LaneLayout(int laneCount, float laneWidth = DefaultLaneWidth)
        {
            if (laneCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(laneCount), "Lane count must be at least 1.");
            }

            if (laneWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(laneWidth), "Lane width must be greater than zero.");
            }

            LaneCount = laneCount;
            LaneWidth = laneWidth;
        }

        public bool IsValidLane(int laneIndex)
        {
            return laneIndex >= 0 && laneIndex < LaneCount;
        }

        public float GetLaneCenterX(int laneIndex)
        {
            return GetLaneCenterX(laneIndex, LaneWidth);
        }

        public float GetLaneCenterX(int laneIndex, float laneWidth)
        {
            if (!IsValidLane(laneIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex), $"Lane index {laneIndex} is outside the valid range [0, {LaneCount - 1}].");
            }

            if (laneWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(laneWidth), "Lane width must be greater than zero.");
            }

            return (laneIndex - ((LaneCount - 1) * 0.5f)) * laneWidth;
        }
    }
}
