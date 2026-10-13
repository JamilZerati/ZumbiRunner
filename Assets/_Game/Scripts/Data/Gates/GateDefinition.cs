using Game.Core.Gates;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(fileName = "GateDefinition", menuName = "Horde Runner/Data/Gate Definition")]
    public class GateDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string perkId;
        [SerializeField] private PerkDefinition perk;
        [SerializeReference] private IGateVariant variant;

        public string Id => id;
        public string DisplayName => displayName;
        public string PerkId => perkId;
        public PerkDefinition Perk => perk;
        public IGateVariant Variant => variant;

        public void SetData(string gateId, string name, string primaryPerkId, PerkDefinition perkDef, IGateVariant gateVariant)
        {
            id = gateId;
            displayName = name;
            perkId = primaryPerkId;
            perk = perkDef;
            variant = gateVariant;
        }
    }
}
