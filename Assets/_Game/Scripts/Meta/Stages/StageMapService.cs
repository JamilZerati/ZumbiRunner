using System;
using System.Collections.Generic;
using Game.Meta.Save;

namespace Game.Meta.Stages
{
    public class StageMapService : IStageMapService
    {
        private readonly List<string> _stageOrder;
        private readonly HashSet<string> _unlockedStages;
        private readonly Dictionary<string, int> _stageStars;
        private readonly HashSet<string> _bossStages = new();

        public StageMapService(MetaState metaState = null, IEnumerable<string> stageOrder = null)
        {
            _stageOrder = stageOrder != null ? new List<string>(stageOrder) : CreateDefaultStages();

            if (metaState != null)
            {
                _unlockedStages = metaState.UnlockedStages;
                _stageStars = metaState.StageStars;
            }
            else
            {
                _unlockedStages = new HashSet<string>();
                _stageStars = new Dictionary<string, int>();
            }

            if (_unlockedStages.Count == 0 && _stageOrder.Count > 0)
            {
                _unlockedStages.Add(_stageOrder[0]);
            }
        }

        public bool IsStageUnlocked(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return false;
            if (_unlockedStages.Contains(stageId)) return true;

            int index = FindStageIndex(stageId);
            if (index >= 0 && _unlockedStages.Contains(_stageOrder[index]))
            {
                return true;
            }

            return false;
        }

        public int GetStars(string stageId)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return 0;
            if (_stageStars.TryGetValue(stageId, out int stars)) return stars;

            int index = FindStageIndex(stageId);
            if (index >= 0 && _stageStars.TryGetValue(_stageOrder[index], out int orderedStars))
            {
                return orderedStars;
            }

            return 0;
        }

        public void RecordStageResult(string stageId, int stars, bool isBoss)
        {
            if (string.IsNullOrWhiteSpace(stageId)) return;

            int clampedStars = Math.Max(0, Math.Min(3, stars));
            int existingStars = GetStars(stageId);
            if (clampedStars > existingStars)
            {
                _stageStars[stageId] = clampedStars;
            }

            if (isBoss)
            {
                _bossStages.Add(stageId);
            }

            if (clampedStars > 0)
            {
                _unlockedStages.Add(stageId);

                int index = FindStageIndex(stageId);
                if (index >= 0 && index + 1 < _stageOrder.Count)
                {
                    _unlockedStages.Add(_stageOrder[index + 1]);
                }
                else if (TryResolveNextSequentialStage(stageId, out string nextStage))
                {
                    _unlockedStages.Add(nextStage);
                }
            }
        }

        public string GetHighestUnlockedStage()
        {
            for (int i = _stageOrder.Count - 1; i >= 0; i--)
            {
                if (_unlockedStages.Contains(_stageOrder[i]))
                {
                    return _stageOrder[i];
                }
            }

            foreach (string stage in _unlockedStages)
            {
                return stage;
            }

            return _stageOrder.Count > 0 ? _stageOrder[0] : "stage_01";
        }

        public IReadOnlyList<string> GetAllBossStages()
        {
            return new List<string>(_bossStages);
        }

        private int FindStageIndex(string stageId)
        {
            int index = _stageOrder.IndexOf(stageId);
            if (index >= 0) return index;

            if (stageId.StartsWith("stage_", StringComparison.OrdinalIgnoreCase))
            {
                string numStr = stageId.Substring("stage_".Length);
                if (int.TryParse(numStr, out int num))
                {
                    string alt1 = $"stage_{num}";
                    string alt2 = $"stage_{num:D2}";
                    index = _stageOrder.IndexOf(alt1);
                    if (index >= 0) return index;
                    index = _stageOrder.IndexOf(alt2);
                    if (index >= 0) return index;
                }
            }

            return -1;
        }

        private static bool TryResolveNextSequentialStage(string stageId, out string nextStageId)
        {
            nextStageId = null;
            if (stageId.StartsWith("stage_", StringComparison.OrdinalIgnoreCase))
            {
                string numStr = stageId.Substring("stage_".Length);
                if (int.TryParse(numStr, out int num))
                {
                    nextStageId = numStr.Length >= 2 ? $"stage_{(num + 1):D2}" : $"stage_{num + 1}";
                    return true;
                }
            }
            return false;
        }

        private static List<string> CreateDefaultStages()
        {
            var list = new List<string>(50);
            for (int i = 1; i <= 50; i++)
            {
                list.Add($"stage_{i:D2}");
            }
            return list;
        }
    }
}
