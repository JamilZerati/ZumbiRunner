using System.Collections.Generic;

namespace Game.Meta.Stages
{
    public interface IStageMapService
    {
        bool IsStageUnlocked(string stageId);
        int GetStars(string stageId);
        void RecordStageResult(string stageId, int stars, bool isBoss);
        string GetHighestUnlockedStage();
        IReadOnlyList<string> GetAllBossStages();
    }
}
