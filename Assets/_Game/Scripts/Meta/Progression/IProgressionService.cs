using Game.Core.State;
using Game.Meta.Rewards;

namespace Game.Meta.Progression
{
    public interface IProgressionService
    {
        bool ProcessRunResult(RunResult result, IRewardSink sink, out IReward grantedReward);
        bool IsContentUnlocked(string contentId);
        bool IsBossDefeated(string levelId);
    }
}
