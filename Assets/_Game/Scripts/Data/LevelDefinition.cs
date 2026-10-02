using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Data/Level Definition", fileName = "LevelDefinition")]
    public class LevelDefinition : ScriptableObject
    {
        public string LevelId;
        public float TotalDistance;
        public float Speed;
        public float TargetDurationSeconds;
        public int InitialTroops;
        public int LaneCount = 2;
        
        public SegmentDefinition[] Segments;
    }
}
