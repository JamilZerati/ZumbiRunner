using System.Collections.Generic;

namespace Game.Core.Allies
{
    public interface IAllyRoster
    {
        IReadOnlyList<IAllyInstance> Allies { get; }
        int HeroCount { get; }
        int PetCount { get; }
        bool TryAddAlly(IAllyInstance ally, out IAllyInstance registeredAlly);
        bool TryGetAlly(string id, out IAllyInstance ally);
        bool ContainsAlly(string id);
        void Clear();
    }
}
