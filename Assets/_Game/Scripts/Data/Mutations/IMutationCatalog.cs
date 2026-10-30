using System.Collections.Generic;

namespace Game.Data.Mutations
{
    public interface IMutationCatalog
    {
        IReadOnlyList<MutationDefinition> Entries { get; }
        IReadOnlyList<MutationDefinition> Mutations { get; }
        bool TryGet(string id, out MutationDefinition definition);
        bool TryGetMutation(string id, out MutationDefinition mutation);
        MutationDefinition GetMutation(string id);
        IReadOnlyDictionary<string, float> GetRewardBonuses();
    }
}
