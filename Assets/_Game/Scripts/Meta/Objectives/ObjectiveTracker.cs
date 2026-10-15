using System;
using System.Collections.Generic;
using Game.Core.State;

namespace Game.Meta.Objectives
{
    public class ObjectiveTracker : IObjectiveTracker
    {
        public void RegisterObjective(ObjectiveDefinition definition) => throw new NotImplementedException();
        public ObjectiveProgress GetProgress(string objectiveId) => throw new NotImplementedException();
        public void IngestEvent<TEvent>(TEvent evt) where TEvent : class => throw new NotImplementedException();
        public void IngestRunResult(RunResult result) => throw new NotImplementedException();
        public IReadOnlyList<string> CheckCompletedObjectives() => throw new NotImplementedException();
    }
}
