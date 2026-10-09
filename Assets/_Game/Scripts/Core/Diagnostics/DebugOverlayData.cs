using System.Collections.Generic;
using Game.Core.Abilities;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Core.Status;

namespace Game.Core.Diagnostics
{
    public struct DebugOverlayData
    {
        public int SquadCount;
        public float DistanceTraveled;
        public float VictoryDistance;
        public string WeaponId;
        public WeaponStats WeaponStats;
        public IReadOnlyList<StatusApplication> ActiveStatuses;
        public StatCollection Stats;
        public IReadOnlyList<GateTriggeredEvent> RecentGates;
        public IReadOnlyList<SynergyTriggeredEvent> RecentSynergies;
        public int GrenadeKills;
        public int GrenadeTargetKills;
        public int GrenadeCharges;
        public HeroAbilityTriggerMode GrenadeMode;
        public int GrenadeUses;
    }
}
