using System;
using System.Collections.Generic;
using Game.Core.State;

namespace Game.Meta.Run
{
    public class RunConfigBuilder : IRunConfigBuilder
    {
        public RunConfig Build(string stageId, IReadOnlyList<string> selectedMutations = null, IReadOnlyList<string> activeBoosts = null) => throw new NotImplementedException();
    }
}
