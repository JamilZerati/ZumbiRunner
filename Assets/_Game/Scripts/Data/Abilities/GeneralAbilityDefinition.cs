using Game.Core.Abilities;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "GeneralAbilityDefinition", menuName = "Horde Runner/Data/General Ability Definition")]
    public class GeneralAbilityDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private int chargeKills;
        [SerializeReference] private IAbilityEffect effect;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int ChargeKills => chargeKills;
        public IAbilityEffect Effect => effect;

        public void SetData(string id, string displayName, string description, int chargeKills, IAbilityEffect effect)
        {
            this.id = id;
            this.displayName = displayName;
            this.description = description;
            this.chargeKills = chargeKills;
            this.effect = effect;
        }
    }
}
