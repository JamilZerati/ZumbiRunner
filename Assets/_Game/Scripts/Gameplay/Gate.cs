using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class Gate : MonoBehaviour
    {
        [SerializeField] private int laneIndex;
        [SerializeField] private PerkDefinition perk;
        [SerializeField] private GatePair parentPair;

        public int LaneIndex
        {
            get => laneIndex;
            set => laneIndex = value;
        }

        public PerkDefinition Perk
        {
            get => perk;
            set => perk = value;
        }

        public GatePair ParentPair
        {
            get => parentPair;
            set => parentPair = value;
        }

        public void Initialize(int lane, PerkDefinition perkDefinition, GatePair pair = null)
        {
            laneIndex = lane;
            perk = perkDefinition;
            if (pair != null)
            {
                parentPair = pair;
                pair.RegisterGate(this);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (other == null || parentPair == null || parentPair.IsConsumed)
            {
                return;
            }

            var squad = other.GetComponentInParent<ISquad>() ?? other.GetComponent<ISquad>();
            if (squad != null)
            {
                parentPair.TryTrigger(laneIndex, squad);
            }
        }
    }
}
