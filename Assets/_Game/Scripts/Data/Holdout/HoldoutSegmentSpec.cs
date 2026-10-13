using System;

namespace Game.Data
{
    [Serializable]
    public class HoldoutSegmentSpec
    {
        public string HoldoutId;
        public int CustomMaxHp;
        public int CustomSurvivors;
        public float CustomDuration;
    }
}
