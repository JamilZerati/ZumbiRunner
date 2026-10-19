using System.Collections.Generic;
using Game.Core;
using Game.Core.Boss;
using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BossBehaviorTests
    {
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

        [Test]
        public void BossPingPongBehavior_SweepsAcrossLanes_AndInvertsAtBoundaries()
        {
            var behavior = new BossPingPongBehavior { MinLane = 0f, MaxLane = 2f, Speed = 2f };
            behavior.Initialize(0f, 1);
            var context = new EnemyBehaviorContext { LaneIndex = 0 };

            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(1f, behavior.CurrentPosition, 1e-4f);
            Assert.AreEqual(1, behavior.Direction);
            Assert.AreEqual(1, context.LaneIndex);

            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(2f, behavior.CurrentPosition, 1e-4f);
            Assert.AreEqual(-1, behavior.Direction);
            Assert.AreEqual(2, context.LaneIndex);

            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(1f, behavior.CurrentPosition, 1e-4f);
            Assert.AreEqual(-1, behavior.Direction);
            Assert.AreEqual(1, context.LaneIndex);

            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(0f, behavior.CurrentPosition, 1e-4f);
            Assert.AreEqual(1, behavior.Direction);
            Assert.AreEqual(0, context.LaneIndex);
        }

        [Test]
        public void BossPingPongBehavior_SynchronizesWithBossCombatState()
        {
            var behavior = new BossPingPongBehavior { MinLane = 0f, MaxLane = 2f, Speed = 2f };
            behavior.Initialize(0f, 1);
            var bossState = new BossCombatRuntimeState();
            bossState.StartArena("boss_colossus", 1000, 0);

            var context = new EnemyBehaviorContext
            {
                LaneIndex = 0,
                BossState = bossState
            };

            behavior.UpdateBehavior(context, 0.5f);
            Assert.AreEqual(1, bossState.CurrentLane);
        }

        [Test]
        public void BossLaneTelegraphDiveBehavior_StartsWarning_AndDivesAtInterval()
        {
            var behavior = new BossLaneTelegraphDiveBehavior
            {
                Interval = 4f,
                TelegraphDuration = 1f,
                Damage = 50
            };
            var squad = new FakeSquad(100);
            var context = new EnemyBehaviorContext
            {
                GeneralLane = 1,
                Squad = squad
            };

            behavior.UpdateBehavior(context, 2.5f);
            Assert.IsFalse(behavior.IsTelegraphing);
            Assert.IsFalse(context.BossTelegraphActive);
            Assert.IsFalse(context.BossDidDive);

            behavior.UpdateBehavior(context, 0.6f); // 3.1s -> within warning window
            Assert.IsTrue(behavior.IsTelegraphing);
            Assert.IsTrue(context.BossTelegraphActive);
            Assert.AreEqual(1, behavior.TargetLane);
            Assert.AreEqual(1, context.BossTelegraphLane);

            behavior.UpdateBehavior(context, 0.9f); // 4.0s -> dive executes
            Assert.IsFalse(behavior.IsTelegraphing);
            Assert.IsFalse(context.BossTelegraphActive);
            Assert.IsTrue(context.BossDidDive);
            Assert.AreEqual(50, context.DamageDealtToSquad);
            Assert.AreEqual(50, squad.SquadCount);
        }

        [Test]
        public void BossLaneTelegraphDiveBehavior_EvadedWhenGeneralChangesLane()
        {
            var behavior = new BossLaneTelegraphDiveBehavior
            {
                Interval = 4f,
                TelegraphDuration = 1f,
                Damage = 50
            };
            var squad = new FakeSquad(100);
            var context = new EnemyBehaviorContext
            {
                GeneralLane = 1,
                Squad = squad
            };

            behavior.UpdateBehavior(context, 3.2f);
            Assert.IsTrue(context.BossTelegraphActive);
            Assert.AreEqual(1, context.BossTelegraphLane);

            // General moves to lane 0 before dive impacts
            context.GeneralLane = 0;
            behavior.UpdateBehavior(context, 0.8f);

            Assert.IsTrue(context.BossDidDive);
            Assert.AreEqual(0, context.DamageDealtToSquad);
            Assert.AreEqual(100, squad.SquadCount);
        }

        [Test]
        public void BossSummonWaveBehavior_TriggersAtHpThresholds_WithoutDuplicates()
        {
            var behavior = new BossSummonWaveBehavior
            {
                Thresholds = new[] { 0.75f, 0.50f, 0.25f },
                MinionArchetypes = new[] { "walker", "runner", "brute" }
            };
            var context = new EnemyBehaviorContext
            {
                MaxHealth = 1000,
                CurrentHealth = 1000
            };

            // 80% HP -> No threshold hit
            context.CurrentHealth = 800;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(0, behavior.TriggeredCount);
            Assert.IsFalse(context.BossSummonTriggered);

            // 70% HP -> 75% threshold hit
            context.CurrentHealth = 700;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(1, behavior.TriggeredCount);
            Assert.IsTrue(behavior.HasTriggered(0.75f));
            Assert.IsTrue(context.BossSummonTriggered);
            Assert.AreEqual(3, context.BossSummonArchetypes.Count);

            // 60% HP -> No new threshold hit, no duplicate
            context.BossSummonTriggered = false;
            context.CurrentHealth = 600;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(1, behavior.TriggeredCount);
            Assert.IsFalse(context.BossSummonTriggered);

            // Heal back to 850 HP and drop to 700 again -> Must not duplicate
            context.CurrentHealth = 850;
            behavior.UpdateBehavior(context, 0.1f);
            context.CurrentHealth = 700;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(1, behavior.TriggeredCount);

            // Drop to 45% HP -> 50% threshold hit
            context.CurrentHealth = 450;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(2, behavior.TriggeredCount);
            Assert.IsTrue(behavior.HasTriggered(0.50f));

            // Drop to 20% HP -> 25% threshold hit
            context.CurrentHealth = 200;
            behavior.UpdateBehavior(context, 0.1f);
            Assert.AreEqual(3, behavior.TriggeredCount);
            Assert.IsTrue(behavior.HasTriggered(0.25f));
        }

        [Test]
        public void BossSummonWaveBehavior_HandlesMultiThresholdDropInSingleHit()
        {
            var behavior = new BossSummonWaveBehavior
            {
                Thresholds = new[] { 0.75f, 0.50f, 0.25f },
                MinionArchetypes = new[] { "walker" }
            };
            var context = new EnemyBehaviorContext
            {
                MaxHealth = 1000,
                CurrentHealth = 1000
            };

            // Massive hit straight down to 40% (crosses 75% and 50%)
            var damage = new DamageInfo(600);
            context.CurrentHealth = 400;
            behavior.OnHit(ref damage, context);

            Assert.AreEqual(2, behavior.TriggeredCount);
            Assert.IsTrue(behavior.HasTriggered(0.75f));
            Assert.IsTrue(behavior.HasTriggered(0.50f));
            Assert.IsFalse(behavior.HasTriggered(0.25f));
        }
    }
}
