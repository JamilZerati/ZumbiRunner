using System;
using System.Collections.Generic;
using Game.Core.State;
using Game.Meta.Rewards;
using Game.Meta.Save;

namespace Game.Meta.Progression
{
    public class ProgressionService : IProgressionService
    {
        private readonly ProgressionCatalog _catalog;
        private readonly MetaState _metaState;
        private readonly HashSet<string> _defeatedBosses = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedContent = new(StringComparer.OrdinalIgnoreCase);

        public ProgressionService(ProgressionCatalog catalog = null, MetaState metaState = null)
        {
            _catalog = catalog ?? ProgressionCatalog.CreateDefaultCatalog();
            _metaState = metaState;

            if (_metaState?.Entitlements != null)
            {
                foreach (string ent in _metaState.Entitlements)
                {
                    _unlockedContent.Add(ent);
                }
            }
        }

        public bool ProcessRunResult(RunResult result, IRewardSink sink, out IReward grantedReward)
        {
            grantedReward = null;
            if (result == null || !result.Victory || sink == null || string.IsNullOrEmpty(result.LevelId))
            {
                return false;
            }

            if (!_catalog.TryGetUnlock(result.LevelId, out var def))
            {
                return false;
            }

            string grantId = $"unlock_boss_{result.LevelId}_{def.ContentId}";
            if (sink.HasGranted(grantId) || _metaState != null && _metaState.ClaimedGrantIds.Contains(grantId))
            {
                return false;
            }

            var reward = new UnlockReward(def.ContentId);
            reward.Apply(sink);
            sink.RecordGrant(grantId);

            _defeatedBosses.Add(result.LevelId);
            _unlockedContent.Add(def.ContentId);

            if (_metaState != null)
            {
                _metaState.ClaimedGrantIds.Add(grantId);
                _metaState.Entitlements.Add(def.ContentId);
            }

            grantedReward = reward;
            return true;
        }

        public bool IsContentUnlocked(string contentId)
        {
            if (string.IsNullOrEmpty(contentId)) return false;
            return _unlockedContent.Contains(contentId);
        }

        public bool IsBossDefeated(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return false;
            return _defeatedBosses.Contains(levelId);
        }
    }
}
