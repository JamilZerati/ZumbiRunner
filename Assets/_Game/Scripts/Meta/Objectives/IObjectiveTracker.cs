using System.Collections.Generic;
using Game.Core.State;

namespace Game.Meta.Objectives
{
    public interface IObjectiveTracker
    {
        void RegisterObjective(ObjectiveDefinition definition);
        ObjectiveProgress GetProgress(string objectiveId);
        void IngestEvent<TEvent>(TEvent evt);
        void IngestRunResult(RunResult result);
        IReadOnlyList<string> CheckCompletedObjectives();
    }
}
