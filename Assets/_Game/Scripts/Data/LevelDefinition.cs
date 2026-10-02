using System.Text.RegularExpressions;
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
        public int Phase = 1;

        public SegmentDefinition[] Segments;

        public static float CalculateHpMultiplier(int phase)
        {
            return 1f + 0.08f * Mathf.Max(0, phase - 1);
        }

        public float GetHpMultiplier(int? phaseOverride = null)
        {
            if (phaseOverride.HasValue)
            {
                return CalculateHpMultiplier(phaseOverride.Value);
            }

            int phase = Phase > 0 ? Phase : ExtractPhaseFromId(LevelId);
            return CalculateHpMultiplier(phase);
        }

        private static int ExtractPhaseFromId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return 1;
            }

            var match = Regex.Match(levelId, @"\d+");
            return match.Success && int.TryParse(match.Value, out int val) ? val : 1;
        }
    }
}
