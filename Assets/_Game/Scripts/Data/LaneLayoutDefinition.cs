using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "LaneLayoutDefinition", menuName = "Horde Runner/Data/Lane Layout Definition")]
    public class LaneLayoutDefinition : ScriptableObject
    {
        [SerializeField, Min(1)] private int laneCount = 2;
        [SerializeField, Min(0.1f)] private float laneWidth = LaneLayout.DefaultLaneWidth;

        public int LaneCount => laneCount;
        public float LaneWidth => laneWidth;

        public LaneLayout ToLayout()
        {
            return new LaneLayout(laneCount, laneWidth);
        }

        public float GetLaneCenterX(int laneIndex)
        {
            return ToLayout().GetLaneCenterX(laneIndex);
        }
    }
}
