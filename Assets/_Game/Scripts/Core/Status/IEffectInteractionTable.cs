using System.Collections.Generic;

namespace Game.Core.Status
{
    public interface IEffectInteractionTable
    {
        IReadOnlyList<EffectInteraction> Interactions { get; }
    }
}
