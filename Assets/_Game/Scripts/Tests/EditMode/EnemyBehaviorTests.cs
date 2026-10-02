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
        public void MoveStraight_DoesNotAdvance_WhenStopped()
        {
            var behavior = new MoveStraightBehavior();
            var context = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 10f),
                Speed = 2f,
                IsStopped = true
            };

            behavior.UpdateBehavior(context, 1f);

            Assert.AreEqual(10f, context.Position.z, 0.001f);
        }

        [Test]
        public void MoveStraight_DoesNotAdvance_WhenEngaged()
        {
            var behavior = new MoveStraightBehavior();
            var context = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 10f),
                Speed = 2f,
                IsEngaged = true
            };

            behavior.UpdateBehavior(context, 1f);

            Assert.AreEqual(10f, context.Position.z, 0.001f);
        }

        [Test]
        public void ChaseLane_DoesNotSwitch_BeforeDelay()
        {
            var behavior = new ChaseLaneBehavior { Delay = 1f };
            var context = new EnemyBehaviorContext
            {
                LaneIndex = 0,
                GeneralLane = 2
            };

            behavior.UpdateBehavior(context, 0.5f);

            Assert.AreEqual(0, context.LaneIndex);
        }

        [Test]
        public void ChaseLane_ResetsTimer_WhenLanesMatch()
        {
            var behavior = new ChaseLaneBehavior { Delay = 1f };
            var context = new EnemyBehaviorContext
            {
                LaneIndex = 0,
                GeneralLane = 2
            };

            behavior.UpdateBehavior(context, 0.8f);
            Assert.AreEqual(0, context.LaneIndex);

            context.GeneralLane = 0;
            behavior.UpdateBehavior(context, 0.1f);

            context.GeneralLane = 1;
            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(0, context.LaneIndex);

            behavior.UpdateBehavior(context, 0.6f);
            Assert.AreEqual(1, context.LaneIndex);
        }

        [Test]
        public void StopAt_ResumesMovement_WhenTargetIsBeyondDistance()
        {
            var behavior = new StopAtBehavior { Distance = 25f };
            var context = new EnemyBehaviorContext
            {
                DistanceToTarget = 30f,
                IsStopped = true
            };

            behavior.UpdateBehavior(context, 0.1f);

            Assert.IsFalse(context.IsStopped);
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
        public void FrontShield_BreaksOnAreaDamage_AndAllowsFullDamage()
        {
            var behavior = new FrontShieldBehavior();
            var context = new EnemyBehaviorContext { ShieldActive = true };
            var aoeHit = new DamageInfo(25, DamageType.Area, null);

            behavior.OnHit(ref aoeHit, context);

            Assert.AreEqual(25, aoeHit.Amount);
            Assert.IsFalse(behavior.IsActive);
            Assert.IsFalse(context.ShieldActive);

            var physicalHit = new DamageInfo(15, DamageType.Physical, null);
            behavior.OnHit(ref physicalHit, context);
            Assert.AreEqual(15, physicalHit.Amount);
        }

        [Test]
        public void FrontShield_BreaksOnElementalStatusDamage()
        {
            var behavior = new FrontShieldBehavior();
            var context = new EnemyBehaviorContext { ShieldActive = true };
            var fireHit = new DamageInfo(10, DamageType.Fire, null);

            behavior.OnHit(ref fireHit, context);

            Assert.AreEqual(10, fireHit.Amount);
            Assert.IsFalse(context.ShieldActive);
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
        public void ExplodeOnContact_ReducesZombieHealthToZero_AndConsumesSquad()
        {
            var behavior = new ExplodeOnContactBehavior { Damage = 30, Radius = 2f };
            var squad = new FakeSquad(50);
            var context = new EnemyBehaviorContext
            {
                CurrentHealth = 30,
                Squad = squad
            };

            behavior.OnEngage(context);

            Assert.AreEqual(0, context.CurrentHealth);
            Assert.AreEqual(30, context.DamageDealtToSquad);
            Assert.AreEqual(20, squad.SquadCount);
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
        public void ExplodeOnDeath_DamagesNearbyZombiesWithinRadius()
        {
            var behavior = new ExplodeOnDeathBehavior { Damage = 50, Radius = 3f };
            var zombieNear = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 2f),
                CurrentHealth = 80
            };
            var zombieFar = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 10f),
                CurrentHealth = 80
            };

            var context = new EnemyBehaviorContext
            {
                Position = Vector3.zero,
                NearbyZombies = new List<EnemyBehaviorContext> { zombieNear, zombieFar }
            };

            behavior.OnDeath(context);

            Assert.AreEqual(50, context.DamageDealtToZombies);
            Assert.AreEqual(30, zombieNear.CurrentHealth);
            Assert.AreEqual(80, zombieFar.CurrentHealth);
        }

        [Test]
        public void RangedSpit_EmitsWarning_AndSpitsOnTargetLane()
        {
            var behavior = new RangedSpitBehavior { Interval = 3f, WarningDuration = 1f, Damage = 30 };
            var context = new EnemyBehaviorContext { GeneralLane = 1 };

            behavior.UpdateBehavior(context, 2.5f);

            Assert.IsTrue(context.SpitWarningActive);
            Assert.AreEqual(1, context.SpitWarningLane);

            behavior.UpdateBehavior(context, 0.6f);

            Assert.IsTrue(context.DidSpit);
            Assert.AreEqual(30, context.DamageDealtToSquad);
            Assert.IsFalse(context.SpitWarningActive);
        }

        [Test]
        public void RangedSpit_EvadedWhenGeneralSwitchesLane()
        {
            var behavior = new RangedSpitBehavior { Interval = 3f, WarningDuration = 1f, Damage = 30 };
            var context = new EnemyBehaviorContext { GeneralLane = 1 };

            behavior.UpdateBehavior(context, 2.5f);
            Assert.IsTrue(context.SpitWarningActive);
            Assert.AreEqual(1, context.SpitWarningLane);

            context.GeneralLane = 2;
            behavior.UpdateBehavior(context, 0.6f);

            Assert.IsTrue(context.DidSpit);
            Assert.AreEqual(0, context.DamageDealtToSquad);
            Assert.IsFalse(context.SpitWarningActive);
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
        public void HealAura_HealsNearbyZombiesWithinRadius()
        {
            var behavior = new HealAuraBehavior { HealPerSecond = 5, Radius = 4f };
            var zombieNear = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 2f),
                CurrentHealth = 10,
                MaxHealth = 20
            };
            var zombieFar = new EnemyBehaviorContext
            {
                Position = new Vector3(0, 0, 6f),
                CurrentHealth = 10,
                MaxHealth = 20
            };

            var context = new EnemyBehaviorContext
            {
                Position = Vector3.zero,
                NearbyZombies = new List<EnemyBehaviorContext> { zombieNear, zombieFar }
            };

            behavior.UpdateBehavior(context, 1f);

            Assert.AreEqual(5, context.HealApplied);
            Assert.AreEqual(15, zombieNear.CurrentHealth);
            Assert.AreEqual(10, zombieFar.CurrentHealth);
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

        [Test]
        public void Resurrect_DoesNotTrigger_BeforeInterval()
        {
            var behavior = new ResurrectBehavior { Interval = 6f, ArchetypeId = "walker" };
            var context = new EnemyBehaviorContext();

            behavior.UpdateBehavior(context, 3f);

            Assert.IsFalse(context.ResurrectTriggered);
        }

        private sealed class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount)
            {
                SquadCount = initialCount;
            }

            public bool Add(int amount)
            {
                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                SquadCount = Mathf.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                SquadCount *= factor;
                return true;
            }

            public bool Divide(int divisor)
            {
                SquadCount /= divisor;
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = newCount;
                return true;
            }
        }
    }
}
