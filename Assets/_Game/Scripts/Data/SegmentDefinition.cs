using System;

namespace Game.Data
{
    public enum SegmentType
    {
        Warmup,
        Gate,
        Horde,
        Multiplier,
        Cage,
        Holdout
    }

    [Serializable]
    public class SegmentEvent
    {
        public float DistanceOffset;
        public string Type;
        public string Data;
        public int Lane = -1;
    }

    [Serializable]
    public class SegmentDefinition
    {
        public SegmentType SegmentType;
        public float StartDistance;
        public float Length;
        public SegmentEvent[] Events;
        public int[] CoveredLanes;
        public CageSpawnSpec[] Cages;
        public HoldoutSegmentSpec Holdout;
        public Game.Core.Weather.WeatherType Weather;
    }
}
