using System.Collections.Generic;
using Game.Core.State;

namespace Game.Meta.Objectives
{
    public interface IObjectiveTracker
    {
        void RegisterObjective(ObjectiveDefinition definition);
        ObjectiveProgress GetProgress(string objectiveId);
        void IngestEvent<TEvent>(TEvent evt) where TEvent : class;
        void IngestRunResult(RunResult result);
        IReadOnlyList<string> CheckCompletedObjectives();
    }
}
