using System.Collections.Generic;

namespace Game.Data.Mutations
{
    public interface IMutationCatalog
    {
        IReadOnlyList<MutationDefinition> Entries { get; }
        bool TryGet(string id, out MutationDefinition definition);
        IReadOnlyDictionary<string, float> GetRewardBonuses();
    }
}
