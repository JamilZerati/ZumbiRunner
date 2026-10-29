using System.Collections.Generic;
using Game.Core.Gates;
using UnityEngine;

namespace Game.Data.Mutations
{
    [CreateAssetMenu(fileName = "MutationDefinition", menuName = "Horde Runner/Data/Mutation Definition")]
    public class MutationDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private float rewardBonus;
        [SerializeReference] private List<IStageModifier> modifiers = new();

        public string Id { get => id; set => id = value; }
        public string DisplayName { get => displayName; set => displayName = value; }
        public string Description { get => description; set => description = value; }
        public float RewardBonus { get => rewardBonus; set => rewardBonus = value; }
        public IReadOnlyList<IStageModifier> Modifiers => modifiers;

        public void SetData(string id, string displayName, string description, float rewardBonus, IEnumerable<IStageModifier> modifiers)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.rewardBonus = rewardBonus;
            this.modifiers = modifiers != null ? new List<IStageModifier>(modifiers) : new List<IStageModifier>();
        }
    }
}
