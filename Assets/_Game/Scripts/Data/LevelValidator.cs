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

                if (segment.Cages != null && segment.Cages.Length > 0)
                {
                    CheckCageSpecs(segment, totalLanes, result.Errors);
                }

                if (segment.SegmentType == SegmentType.Holdout || (segment.Holdout != null && !string.IsNullOrWhiteSpace(segment.Holdout.HoldoutId)))
                {
                    CheckHoldoutSpec(segment, result.Errors);
                }

                if (segment.SegmentType == SegmentType.Boss || (segment.Boss != null && !string.IsNullOrWhiteSpace(segment.Boss.BossId)))
                {
                    CheckBossSpec(segment, totalLanes, result.Errors);
                }
            }

            CheckSegmentOverlaps(level, result.Errors);

            return result;
        }

        private static void CheckCageSpecs(SegmentDefinition segment, int totalLanes, List<string> errors)
        {
            for (int c = 0; c < segment.Cages.Length; c++)
            {
                var spec = segment.Cages[c];
                if (spec == null)
                {
                    errors.Add($"Cage spec at index {c} in segment at {segment.StartDistance:F1}m is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(spec.CageId))
                {
                    errors.Add($"Cage spec at index {c} in segment at {segment.StartDistance:F1}m has empty CageId.");
                }

                if (spec.LaneIndex < 0 || spec.LaneIndex >= totalLanes)
                {
                    errors.Add($"Cage '{spec.CageId}' in segment at {segment.StartDistance:F1}m has invalid LaneIndex {spec.LaneIndex}. Total lanes: {totalLanes}.");
                }

                if (spec.DistanceOffset < 0f || spec.DistanceOffset > segment.Length)
                {
                    errors.Add($"Cage '{spec.CageId}' in segment at {segment.StartDistance:F1}m has invalid DistanceOffset ({spec.DistanceOffset:F1}m). Must be between 0 and {segment.Length:F1}m.");
                }

                if (spec.CustomHp < 0)
                {
                    errors.Add($"Cage '{spec.CageId}' in segment at {segment.StartDistance:F1}m has negative CustomHp ({spec.CustomHp}).");
                }

                if (spec.CustomSurvivors < 0)
                {
                    errors.Add($"Cage '{spec.CageId}' in segment at {segment.StartDistance:F1}m has negative CustomSurvivors ({spec.CustomSurvivors}).");
                }
            }
        }

        private static void CheckHoldoutSpec(SegmentDefinition segment, List<string> errors)
        {
            if (segment.SegmentType == SegmentType.Holdout)
            {
                if (segment.Holdout == null)
                {
                    errors.Add($"Holdout segment at {segment.StartDistance:F1}m has no Holdout spec.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(segment.Holdout.HoldoutId))
                {
                    errors.Add($"Holdout spec in segment at {segment.StartDistance:F1}m has empty HoldoutId.");
                }
            }

            if (segment.Holdout != null && !string.IsNullOrWhiteSpace(segment.Holdout.HoldoutId))
            {
                var spec = segment.Holdout;
                if (spec.CustomMaxHp < 0)
                {
                    errors.Add($"Holdout '{spec.HoldoutId}' in segment at {segment.StartDistance:F1}m has negative CustomMaxHp ({spec.CustomMaxHp}).");
                }

                if (spec.CustomSurvivors < 0)
                {
                    errors.Add($"Holdout '{spec.HoldoutId}' in segment at {segment.StartDistance:F1}m has negative CustomSurvivors ({spec.CustomSurvivors}).");
                }

                if (spec.CustomDuration < 0f)
                {
                    errors.Add($"Holdout '{spec.HoldoutId}' in segment at {segment.StartDistance:F1}m has negative CustomDuration ({spec.CustomDuration:F1}s).");
                }
            }
        }

        private static void CheckBossSpec(SegmentDefinition segment, int totalLanes, List<string> errors)
        {
            if (segment.SegmentType == SegmentType.Boss)
            {
                if (segment.Boss == null)
                {
                    errors.Add($"Boss segment at {segment.StartDistance:F1}m has no Boss spec.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(segment.Boss.BossId))
                {
                    errors.Add($"Boss spec in segment at {segment.StartDistance:F1}m has empty BossId.");
                }
            }

            if (segment.Boss != null)
            {
                var spec = segment.Boss;
                string bossName = string.IsNullOrWhiteSpace(spec.BossId) ? "Unknown" : spec.BossId;
                if (spec.CustomMaxHp < 0)
                {
                    errors.Add($"Boss '{bossName}' in segment at {segment.StartDistance:F1}m has negative CustomMaxHp ({spec.CustomMaxHp}).");
                }

                if (spec.InitialLane < 0 || spec.InitialLane >= totalLanes)
                {
                    errors.Add($"Boss '{bossName}' in segment at {segment.StartDistance:F1}m has invalid InitialLane {spec.InitialLane}. Total lanes: {totalLanes}.");
                }

                if (spec.ArenaLength < 0f)
                {
                    errors.Add($"Boss '{bossName}' in segment at {segment.StartDistance:F1}m has negative ArenaLength ({spec.ArenaLength:F1}m).");
                }
            }
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
