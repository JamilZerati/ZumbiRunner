using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class GatePair : MonoBehaviour
    {
        [SerializeField] private List<Gate> gates = new();

        private IEventBus eventBus;
        private StageModifierController stageModifierController;

        public event Action<int, Gate> OnGateTriggered;

        public bool IsConsumed { get; private set; }
        public IReadOnlyList<Gate> Gates => gates;

        public StageModifierController StageModifierController
        {
            get => stageModifierController != null ? stageModifierController : (stageModifierController = FindAnyObjectByType<LevelDirector>()?.StageModifierController);
            set => stageModifierController = value;
        }

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

        public void Initialize(IEnumerable<Gate> gateList, IEventBus bus = null, StageModifierController stageModifiers = null)
        {
            gates = gateList != null ? new List<Gate>(gateList) : new List<Gate>();
            eventBus = bus;
            stageModifierController = stageModifiers;
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

        public bool TryTrigger(
            int laneIndex,
            ISquad squad,
            IEventBus bus = null,
            IWeaponLoadout loadout = null,
            StageModifierController stageModifiers = null,
            float currentDistance = 0f,
            IAllyRoster roster = null)
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

            var activeBus = bus ?? eventBus;
            var activeModifierController = stageModifiers ?? StageModifierController;
            var runtimeState = targetGate.RuntimeState ?? targetGate.EnsureRuntimeState();
            var variant = targetGate.Variant;
            var variantType = runtimeState != null
                ? runtimeState.VariantType
                : (variant != null ? variant.VariantType : GateVariantType.Static);

            int appliedValue = 0;

            if (variantType == GateVariantType.ShootToUpgrade)
            {
                appliedValue = variant != null
                    ? variant.ApplyToSquad(squad, runtimeState)
                    : ApplySquadDelta(squad, runtimeState != null ? runtimeState.CurrentValue : 0);
            }
            else
            {
                var activeRoster = roster ?? (squad as IAllyRoster);
                if (targetGate.Perk != null)
                {
                    ApplyPerk(targetGate.Perk, squad, laneIndex, loadout, activeRoster, activeModifierController);
                }

                if (variantType == GateVariantType.Cursed)
                {
                    if (targetGate.BonusPerk != null)
                    {
                        ApplyPerk(targetGate.BonusPerk, squad, laneIndex, loadout, activeRoster, activeModifierController);
                    }

                    if (variant is CursedGateVariant cursedVariant && activeModifierController != null)
                    {
                        float distance = currentDistance > 0f ? currentDistance : targetGate.transform.position.z;
                        cursedVariant.ActivateModifier(activeModifierController, distance);
                    }
                }

                appliedValue = variant != null ? variant.ApplyToSquad(squad, runtimeState, 0) : 0;
            }

            string gateId = targetGate.GateId;
            string perkId = targetGate.Perk != null && targetGate.Perk.Id != null
                ? targetGate.Perk.Id
                : (targetGate.Definition != null ? targetGate.Definition.PerkId ?? string.Empty : string.Empty);

            activeBus?.Publish(new GatePassedEvent(gateId, perkId, variantType, laneIndex, appliedValue));
            activeBus?.Publish(new GateTriggeredEvent(laneIndex, perkId));

            OnGateTriggered?.Invoke(laneIndex, targetGate);
            return true;
        }

        private static void ApplyPerk(
            PerkDefinition perk,
            ISquad squad,
            int laneIndex,
            IWeaponLoadout loadout,
            IAllyRoster roster,
            StageModifierController stageModifiers)
        {
            if (perk == null)
            {
                return;
            }

            var effects = perk.Effects;
            if (effects == null || effects.Count == 0)
            {
                return;
            }

            var context = new PerkContext(squad, laneIndex, loadout, roster: roster);
            if (context.SourceId == null)
            {
                context = context.WithSourceId(perk.Id);
            }

            bool remapMult = stageModifiers != null && stageModifiers.RemapGateMultiplication;
            bool remapDiv = stageModifiers != null && stageModifiers.RemapGateDivision;

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                if (remapMult && effect is MultiplySoldiersEffect mult)
                {
                    if (mult.Factor > 0)
                    {
                        squad?.Add(mult.Factor);
                    }
                }
                else if (remapDiv && effect is DivideSoldiersEffect div)
                {
                    if (div.Divisor > 0)
                    {
                        ApplySquadDelta(squad, -div.Divisor);
                    }
                }
                else
                {
                    effect.Apply(context);
                }
            }
        }

        private static int ApplySquadDelta(ISquad squad, int value)
        {
            if (squad == null)
            {
                return 0;
            }

            if (value > 0)
            {
                squad.Add(value);
            }
            else if (value < 0)
            {
                int penalty = -value;
                int maxRemovable = Math.Max(0, squad.SquadCount - 1);
                int toRemove = Math.Min(penalty, maxRemovable);
                if (toRemove > 0)
                {
                    squad.Remove(toRemove);
                }
            }

            return value;
        }
    }
}
