using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    public class EnemyBehaviorContext
    {
        public string ArchetypeId { get; set; } = "walker";
        public int LaneIndex { get; set; }
        public Vector3 Position { get; set; }
        public float Speed { get; set; } = 2f;
        public int CurrentHealth { get; set; } = 20;
        public int MaxHealth { get; set; } = 20;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsEngaged { get; set; }

        public int GeneralLane { get; set; }
        public float DistanceToTarget { get; set; }
        public bool IsStopped { get; set; }

        public bool ShieldActive { get; set; } = true;

        public bool SpitWarningActive { get; set; }
        public int SpitWarningLane { get; set; }
        public bool DidSpit { get; set; }

        public int DamageDealtToSquad { get; set; }
        public int DamageDealtToZombies { get; set; }
        public int HealApplied { get; set; }
        public bool ResurrectTriggered { get; set; }
        public string ResurrectArchetypeId { get; set; }

        public ISquad Squad { get; set; }
        public List<EnemyBehaviorContext> NearbyZombies { get; set; } = new();
    }

    public interface IEnemyBehavior
    {
        void UpdateBehavior(EnemyBehaviorContext context, float deltaTime);
        void OnHit(ref DamageInfo hit, EnemyBehaviorContext context);
        void OnEngage(EnemyBehaviorContext context);
        void OnDeath(EnemyBehaviorContext context);
    }
}
