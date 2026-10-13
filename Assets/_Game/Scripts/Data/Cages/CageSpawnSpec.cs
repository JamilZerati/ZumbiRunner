using System;

namespace Game.Data
{
    [Serializable]
    public class CageSpawnSpec
    {
        public string CageId;
        public int LaneIndex;
        public float DistanceOffset;
        public int CustomHp;
        public int CustomSurvivors;
    }
}
