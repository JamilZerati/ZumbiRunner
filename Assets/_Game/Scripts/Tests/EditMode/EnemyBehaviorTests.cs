using System.Collections.Generic;
using Game.Core;
using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EnemyBehaviorTests
    {
        private readonly List<Object> _objectsToDestroy = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _objectsToDestroy.Count; i++)
            {
                if (_objectsToDestroy[i] != null)
                {
                    Object.DestroyImmediate(_objectsToDestroy[i]);
                }
            }
            _objectsToDestroy.Clear();
        }

        [Test]
        public void MoveStraight_AdvancesOnCurrentLane()
        {
            var behavior = new MoveStraightBehavior();
            var context = new EnemyBehaviorContext
            {
                LaneIndex = 1,
                Position = new Vector3(0, 0, 10f),
                Speed = 2f
            };

            behavior.UpdateBehavior(context, 1f);

            Assert.AreEqual(8f, context.Position.z, 0.001f);
        }

        [Test]
        public void ChaseLane_SwitchesToGeneralLane_AfterDelay()
        {
            var behavior = new ChaseLaneBehavior { Delay = 1f };
            var context = new EnemyBehaviorContext
            {
                LaneIndex = 0,
                GeneralLane = 2,
                Position = new Vector3(0, 0, 10f)
            };

            behavior.UpdateBehavior(context, 1.1f);

            Assert.AreEqual(2, context.LaneIndex);
        }

        [Test]
        public void StopAt_HaltsBeforeTargetDistance()
        {
            var behavior = new StopAtBehavior { Distance = 25f };
            var context = new EnemyBehaviorContext
            {
                DistanceToTarget = 20f,
                IsStopped = false
            };

            behavior.UpdateBehavior(context, 0.1f);

            Assert.IsTrue(context.IsStopped);
        }

        [Test]
        public void FrontShield_BlocksStraightProjectiles_UntilStatusOrAoe()
        {
            var behavior = new FrontShieldBehavior();
            var context = new EnemyBehaviorContext { ShieldActive = true };
            var hit = new DamageInfo(20, DamageType.Physical, null);

            behavior.OnHit(ref hit, context);

            Assert.AreEqual(0, hit.Amount);
        }

        [Test]
        public void ExplodeOnContact_DamagesSquad_OnEngagement()
        {
            var behavior = new ExplodeOnContactBehavior { Damage = 30, Radius = 2f };
            var context = new EnemyBehaviorContext();

            behavior.OnEngage(context);

            Assert.AreEqual(30, context.DamageDealtToSquad);
        }

        [Test]
        public void ExplodeOnDeath_DamagesAdjacentZombies_OnKilled()
        {
            var behavior = new ExplodeOnDeathBehavior { Damage = 50, Radius = 3f };
            var context = new EnemyBehaviorContext();

            behavior.OnDeath(context);

            Assert.AreEqual(50, context.DamageDealtToZombies);
        }

        [Test]
        public void RangedSpit_EmitsWarning_AndSpitsOnTargetLane()
        {
            var behavior = new RangedSpitBehavior { Interval = 3f, WarningDuration = 1f, Damage = 30 };
            var context = new EnemyBehaviorContext { GeneralLane = 1 };

            behavior.UpdateBehavior(context, 2.5f);

            Assert.IsTrue(context.SpitWarningActive);
        }

        [Test]
        public void HealAura_HealsNearbyZombies()
        {
            var behavior = new HealAuraBehavior { HealPerSecond = 5, Radius = 4f };
            var context = new EnemyBehaviorContext();

            behavior.UpdateBehavior(context, 1f);

            Assert.AreEqual(5, context.HealApplied);
        }

        [Test]
        public void Resurrect_RevivesWalker_Periodically()
        {
            var behavior = new ResurrectBehavior { Interval = 6f, ArchetypeId = "walker" };
            var context = new EnemyBehaviorContext();

            behavior.UpdateBehavior(context, 6.1f);

            Assert.IsTrue(context.ResurrectTriggered);
            Assert.AreEqual("walker", context.ResurrectArchetypeId);
        }
    }
}
