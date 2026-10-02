using System;

namespace Game.Data
{
    public enum SegmentType
    {
        Warmup,
        Gate,
        Horde,
        Multiplier
    }

    [Serializable]
    public class SegmentEvent
    {
        public float DistanceOffset;
        public string Type;
        public string Data;
    }

    [Serializable]
    public class SegmentDefinition
    {
        public SegmentType SegmentType;
        public float StartDistance;
        public float Length;
        public SegmentEvent[] Events;
    }
}
