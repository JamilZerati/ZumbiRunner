using System.Collections.Generic;
using Game.Core.State;

namespace Game.Meta.Run
{
    public interface IRunConfigBuilder
    {
        RunConfig Build(string stageId, IReadOnlyList<string> selectedMutations = null, IReadOnlyList<string> activeBoosts = null);
    }
}
