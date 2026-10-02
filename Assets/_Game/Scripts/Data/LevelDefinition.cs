using System.Collections.Generic;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        public string Id;
        public List<SegmentDefinition> Segments;
    }
}
