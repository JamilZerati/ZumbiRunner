using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Data
{
    public class LevelValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new List<string>();

        public static implicit operator bool(LevelValidationResult result) => result != null && result.IsValid;
    }

    public static class LevelValidator
    {
        public const float MinimumHordeOverlap = 8.0f;

        public static LevelValidationResult Validate(LevelDefinition level)
        {
            var result = new LevelValidationResult();
            if (level == null)
            {
                result.Errors.Add("Level definition is null.");
                return result;
            }

            if (level.TotalDistance <= 0f)
            {
                result.Errors.Add($"Level '{level.LevelId}' TotalDistance must be greater than zero.");
            }

            if (level.Segments == null || level.Segments.Length == 0)
            {
                result.Errors.Add($"Level '{level.LevelId}' has no segments defined.");
                return result;
            }

            int totalLanes = level.LaneCount > 0 ? level.LaneCount : 2;

            for (int i = 0; i < level.Segments.Length; i++)
            {
                var segment = level.Segments[i];
                if (segment == null)
                {
                    continue;
                }

                if (segment.SegmentType == SegmentType.Horde)
                {
                    CheckHordeSegmentLanes(segment, totalLanes, result.Errors);
                }
            }

            CheckSegmentOverlaps(level, result.Errors);

            return result;
        }

        private static void CheckHordeSegmentLanes(SegmentDefinition segment, int totalLanes, List<string> errors)
        {
            var coveredLanes = new HashSet<int>();

            if (segment.CoveredLanes != null)
            {
                for (int i = 0; i < segment.CoveredLanes.Length; i++)
                {
                    int lane = segment.CoveredLanes[i];
                    if (lane >= 0 && lane < totalLanes)
                    {
                        coveredLanes.Add(lane);
                    }
                }
            }

            if (segment.Events != null)
            {
                for (int i = 0; i < segment.Events.Length; i++)
                {
                    var evt = segment.Events[i];
                    if (evt == null)
                    {
                        continue;
                    }

                    if (evt.Lane >= 0 && evt.Lane < totalLanes)
                    {
                        coveredLanes.Add(evt.Lane);
                    }

                    ExtractLanesFromData(evt.Data, totalLanes, coveredLanes);
                }
            }

            for (int l = 0; l < totalLanes; l++)
            {
                if (!coveredLanes.Contains(l))
                {
                    errors.Add($"Horde segment at {segment.StartDistance:F1}m has empty lane {l}. All {totalLanes} lanes must be covered.");
                }
            }
        }

        private static void ExtractLanesFromData(string data, int totalLanes, HashSet<int> coveredLanes)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var matches = Regex.Matches(data, @"lane[s]?\s*[:=]\s*(\d+(?:\s*,\s*\d+)*)", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                string numbers = match.Groups[1].Value;
                string[] parts = numbers.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i].Trim(), out int lane) && lane >= 0 && lane < totalLanes)
                    {
                        coveredLanes.Add(lane);
                    }
                }
            }

            if (int.TryParse(data.Trim(), out int singleLane) && singleLane >= 0 && singleLane < totalLanes)
            {
                coveredLanes.Add(singleLane);
            }
        }

        private static void CheckSegmentOverlaps(LevelDefinition level, List<string> errors)
        {
            if (level.Segments == null || level.Segments.Length < 2)
            {
                return;
            }

            for (int i = 0; i < level.Segments.Length; i++)
            {
                var segA = level.Segments[i];
                if (segA == null)
                {
                    continue;
                }

                for (int j = i + 1; j < level.Segments.Length; j++)
                {
                    var segB = level.Segments[j];
                    if (segB == null)
                    {
                        continue;
                    }

                    bool involvesHordeAndInteraction =
                        (segA.SegmentType == SegmentType.Horde && (segB.SegmentType == SegmentType.Gate || segB.SegmentType == SegmentType.Horde)) ||
                        (segB.SegmentType == SegmentType.Horde && (segA.SegmentType == SegmentType.Gate || segA.SegmentType == SegmentType.Horde));

                    if (!involvesHordeAndInteraction)
                    {
                        continue;
                    }

                    float startA = segA.StartDistance;
                    float endA = segA.StartDistance + segA.Length;
                    float startB = segB.StartDistance;
                    float endB = segB.StartDistance + segB.Length;

                    float overlapStart = Mathf.Max(startA, startB);
                    float overlapEnd = Mathf.Min(endA, endB);
                    float overlap = overlapEnd - overlapStart;

                    if (overlap > 0f && overlap < MinimumHordeOverlap)
                    {
                        errors.Add($"Segments '{segA.SegmentType}' ({startA:F1}m..{endA:F1}m) and '{segB.SegmentType}' ({startB:F1}m..{endB:F1}m) have overlap of {overlap:F1}m, which is less than the required minimum {MinimumHordeOverlap:F1}m.");
                    }
                }
            }
        }
    }
}
