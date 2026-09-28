using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Perks;
using Game.Core.Stats;
using UnityEngine;

namespace Game.Gameplay
{
    public class GatePair : MonoBehaviour
    {
        [SerializeField] private List<Gate> gates = new();

        private IEventBus eventBus;

        public event Action<int, Gate> OnGateTriggered;

        public bool IsConsumed { get; private set; }
        public IReadOnlyList<Gate> Gates => gates;

        private void Awake()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null && gates[i].ParentPair == null)
                {
                    gates[i].ParentPair = this;
                }
            }
        }

        public void Initialize(IEnumerable<Gate> gateList, IEventBus bus = null)
        {
            gates = gateList != null ? new List<Gate>(gateList) : new List<Gate>();
            eventBus = bus;
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                {
                    gates[i].ParentPair = this;
                }
            }
        }

        public void RegisterGate(Gate gate)
        {
            if (gate == null)
            {
                return;
            }

            if (!gates.Contains(gate))
            {
                gates.Add(gate);
            }

            gate.ParentPair = this;
        }

        public bool TryTrigger(int laneIndex, ISquad squad, IEventBus bus = null, IWeaponLoadout loadout = null)
        {
            if (IsConsumed || squad == null)
            {
                return false;
            }

            Gate targetGate = null;
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null && gates[i].LaneIndex == laneIndex)
                {
                    targetGate = gates[i];
                    break;
                }
            }

            if (targetGate == null)
            {
                return false;
            }

            IsConsumed = true;

            if (targetGate.Perk != null)
            {
                var context = new PerkContext(squad, laneIndex);
                targetGate.Perk.Apply(context);
            }

            string perkId = targetGate.Perk != null && targetGate.Perk.Id != null
                ? targetGate.Perk.Id
                : string.Empty;

            var activeBus = bus ?? eventBus;
            activeBus?.Publish(new GateTriggeredEvent(laneIndex, perkId));

            OnGateTriggered?.Invoke(laneIndex, targetGate);
            return true;
        }
    }
}
