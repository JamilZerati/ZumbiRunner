using System;
using System.Collections.Generic;

namespace Game.Meta.Stages
{
    public class StageMapService : IStageMapService
    {
        public bool IsStageUnlocked(string stageId) => throw new NotImplementedException();
        public int GetStars(string stageId) => throw new NotImplementedException();
        public void RecordStageResult(string stageId, int stars, bool isBoss) => throw new NotImplementedException();
        public string GetHighestUnlockedStage() => throw new NotImplementedException();
        public IReadOnlyList<string> GetAllBossStages() => throw new NotImplementedException();
    }
}
