using System;
using Game.Core;
using Game.Core.Allies;
using Game.Core.Gates;
using Game.Core.Stats;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class Gate : MonoBehaviour, IDamageable
    {
        [SerializeField] private int laneIndex;
        [SerializeField] private PerkDefinition perk;
        [SerializeField] private PerkDefinition bonusPerk;
        [SerializeField] private GateDefinition definition;
        [SerializeField] private GatePair parentPair;
        [SerializeField] private string gateId;

        private IGateVariant variantOverride;
        private GateRuntimeState runtimeState;

        public event Action<Gate> OnGateStateChanged;

        public int LaneIndex
        {
            get => laneIndex;
            set => laneIndex = value;
        }

        public string GateId
        {
            get => definition != null && !string.IsNullOrEmpty(definition.Id) ? definition.Id : (gateId ?? string.Empty);
            set => gateId = value;
        }

        public PerkDefinition Perk
        {
            get => perk != null ? perk : (definition != null ? definition.Perk : null);
            set
            {
                perk = value;
                runtimeState = null;
            }
        }

        public PerkDefinition BonusPerk
        {
            get => bonusPerk;
            set => bonusPerk = value;
        }

        public GateDefinition Definition
        {
            get => definition;
            set
            {
                definition = value;
                if (value != null)
                {
                    if (value.Perk != null)
                    {
                        perk = value.Perk;
                    }
                    if (value.Variant != null)
                    {
                        variantOverride = value.Variant;
                        runtimeState = value.Variant.CreateInitialState(value.DisplayName);
                    }
                }
                else
                {
                    runtimeState = null;
                }
            }
        }

        public IGateVariant Variant
        {
            get => definition != null && definition.Variant != null ? definition.Variant : variantOverride;
            set
            {
                variantOverride = value;
                runtimeState = null;
            }
        }

        public GateRuntimeState RuntimeState => EnsureRuntimeState();

        public GatePair ParentPair
        {
            get => parentPair;
            set => parentPair = value;
        }

        public int CurrentHealth => 1;
        public int MaxHealth => 1;

        // Projéteis colidem exclusivamente com portão ShootToUpgrade ativo para não bloquear zumbis atrás de portões normais.
        public bool IsAlive => (parentPair == null || !parentPair.IsConsumed)
            && EnsureRuntimeState().VariantType == GateVariantType.ShootToUpgrade;

        public void Initialize(int lane, PerkDefinition perkDefinition, GatePair pair = null, PerkDefinition bonusPerkDefinition = null)
        {
            laneIndex = lane;
            perk = perkDefinition;
            bonusPerk = bonusPerkDefinition;
            runtimeState = null;
            EnsureRuntimeState();

            if (pair != null)
            {
                parentPair = pair;
                pair.RegisterGate(this);
            }
        }

        public void Initialize(int lane, GateDefinition gateDefinition, GatePair pair = null, PerkDefinition bonusPerkDefinition = null)
        {
            definition = gateDefinition;
            laneIndex = lane;
            perk = gateDefinition != null ? gateDefinition.Perk : null;
            bonusPerk = bonusPerkDefinition;
            runtimeState = null;
            EnsureRuntimeState();

            if (pair != null)
            {
                parentPair = pair;
                pair.RegisterGate(this);
            }
        }

        public void SetVariant(IGateVariant variant, string displayText = null)
        {
            variantOverride = variant;
            runtimeState = variant != null ? variant.CreateInitialState(displayText) : null;
            NotifyStateChanged();
        }

        public GateRuntimeState EnsureRuntimeState()
        {
            if (runtimeState == null)
            {
                var activeVariant = Variant;
                if (activeVariant != null)
                {
                    string display = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                        ? definition.DisplayName
                        : (perk != null ? perk.DisplayName : string.Empty);
                    runtimeState = activeVariant.CreateInitialState(display);
                }
                else
                {
                    string display = perk != null ? perk.DisplayName : (definition != null ? definition.DisplayName : string.Empty);
                    runtimeState = new GateRuntimeState(GateVariantType.Static, false, 0, display);
                }
            }

            return runtimeState;
        }

        public void TakeDamage(DamageInfo damage)
        {
            if (!IsAlive)
            {
                return;
            }

            var state = EnsureRuntimeState();
            var activeVariant = Variant;
            if (activeVariant != null)
            {
                activeVariant.RegisterHit(state, 1);
            }

            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnGateStateChanged?.Invoke(this);
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
                var loadout = other.GetComponentInParent<IWeaponLoadout>() ?? other.GetComponent<IWeaponLoadout>();
                var roster = other.GetComponentInParent<IAllyRoster>() ?? other.GetComponent<IAllyRoster>();
                parentPair.TryTrigger(laneIndex, squad, loadout: loadout, roster: roster);
            }
        }
    }
}
