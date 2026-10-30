using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Boss;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.Modifiers;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.State;
using Game.Data;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class MutationGameplayModifiersTests
    {
        private List<Object> _toDestroy;

        [SetUp]
        public void SetUp()
        {
            _toDestroy = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_toDestroy != null)
            {
                for (int i = _toDestroy.Count - 1; i >= 0; i--)
                {
                    if (_toDestroy[i] != null)
                    {
                        Object.DestroyImmediate(_toDestroy[i]);
                    }
                }
                _toDestroy.Clear();
            }
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null)
            {
                _toDestroy.Add(obj);
            }
            return obj;
        }

        private PerkDefinition CreatePerk(string id, string name, params IPerkEffect[] effects)
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData(id, name, new List<IPerkEffect>(effects));
            return perk;
        }

        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount = 0)
            {
                SquadCount = initialCount;
            }

            public bool Add(int count)
            {
                SquadCount += count;
                return true;
            }

            public bool Remove(int count)
            {
                SquadCount = Math.Max(0, SquadCount - count);
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
                SquadCount = Math.Max(0, newCount);
                return true;
            }
        }

        [Test]
        public void CT_3_Blindados_MultipliesEnemyBaseHealth_ByOnePointFive()
        {
            var controller = new StageModifierController();
            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            controller.Activate(armored, startDistance: 0f, durationDistance: float.MaxValue);

            int regularEnemyBaseHp = 100;
            int effectiveRegularEnemyHp = (int)Math.Round(regularEnemyBaseHp * controller.CurrentEnemyHealthMultiplier);
            Assert.AreEqual(150, effectiveRegularEnemyHp);

            int bossBaseHp = 1000;
            int effectiveBossHp = (int)Math.Round(bossBaseHp * controller.CurrentEnemyHealthMultiplier);
            Assert.AreEqual(1500, effectiveBossHp);
        }

        [Test]
        public void CT_4_Frenesi_IncreasesEnemyMoveSpeed_ByThirtyPercent()
        {
            var controller = new StageModifierController();
            var frenzy = new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f);
            controller.Activate(frenzy, startDistance: 0f, durationDistance: float.MaxValue);

            float enemyBaseSpeed = 2.0f;
            float effectiveSpeed = enemyBaseSpeed * controller.CurrentEnemySpeedMultiplier;
            Assert.AreEqual(2.6f, effectiveSpeed, 1e-4f);
        }

        [Test]
        public void CT_5_Neblina_ReducesSquadEngagementRange_ByFortyPercent()
        {
            var controller = new StageModifierController();
            var fog = new SquadStatStageModifier("mutacao:fog", StatTarget.Range, 0.6f);
            controller.Activate(fog, startDistance: 0f, durationDistance: float.MaxValue);

            float squadBaseRange = 10.0f;
            float effectiveRange = squadBaseRange * controller.CurrentSquadRangeMultiplier;
            Assert.AreEqual(6.0f, effectiveRange, 1e-4f);

            float reductionPercentage = (1.0f - controller.CurrentSquadRangeMultiplier) * 100f;
            Assert.AreEqual(40.0f, reductionPercentage, 1e-4f);
        }

        [Test]
        public void CT_6_UltimoSoldado_ForcesInitialSquadToOne_OverridingBaseConfig()
        {
            var controller = new StageModifierController();
            var lastSoldier = new InitialSquadOverrideStageModifier("mutacao:last_soldier", 1);
            controller.Activate(lastSoldier, startDistance: 0f, durationDistance: float.MaxValue);

            var runConfig = new RunConfig("stage_01", initialSquad: 15, "pistol", 1f, null);

            int effectiveInitialSquad = controller.CurrentInitialSquadOverride ?? runConfig.InitialSquad;
            Assert.AreEqual(1, effectiveInitialSquad);

            var highUpgradeConfig = new RunConfig("stage_01", initialSquad: 50, "rifle", 1f, null);
            int effectiveHighSquad = controller.CurrentInitialSquadOverride ?? highUpgradeConfig.InitialSquad;
            Assert.AreEqual(1, effectiveHighSquad);
        }

        [Test]
        public void CT_7_SemMultiplicacao_RemapsMultiplyToAdd_AndDivideToSubtract_WithMinimumSquadClamp()
        {
            var controller = new StageModifierController();
            var noMult = new GateOperationRemapStageModifier(
                "mutacao:no_multiplication",
                remapMultiplyToAdd: true,
                remapDivideToSubtract: true);
            controller.Activate(noMult, startDistance: 0f, durationDistance: float.MaxValue);

            Assert.IsTrue(controller.RemapGateMultiplication);
            Assert.IsTrue(controller.RemapGateDivision);

            int squad = 10;
            int multFactor = 2;
            int squadAfterMultiply = controller.RemapGateMultiplication ? squad + multFactor : squad * multFactor;
            Assert.AreEqual(12, squadAfterMultiply);

            int squadAfterMult3 = controller.RemapGateMultiplication ? 5 + 3 : 5 * 3;
            Assert.AreEqual(8, squadAfterMult3);

            int divFactor = 2;
            int squadAfterDivide = controller.RemapGateDivision ? Math.Max(1, squad - divFactor) : Math.Max(1, squad / divFactor);
            Assert.AreEqual(8, squadAfterDivide);

            int smallSquad = 3;
            int largeDivFactor = 5;
            int squadAfterExcessiveDiv = controller.RemapGateDivision
                ? Math.Max(1, smallSquad - largeDivFactor)
                : Math.Max(1, smallSquad / largeDivFactor);
            Assert.AreEqual(1, squadAfterExcessiveDiv);
        }

        [Test]
        public void GatePair_WithRemapGateMultiplication_ConvertsMultiplyToAdd()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();
            var gateGo = Track(new GameObject("Gate"));
            var gate = gateGo.AddComponent<Gate>();
            var perk = CreatePerk("mult_2", "x2", new MultiplySoldiersEffect(2));
            gate.Initialize(0, perk, pair);

            var controller = new StageModifierController();
            var noMult = new GateOperationRemapStageModifier("mutacao:no_multiplication", true, false);
            controller.Activate(noMult, 0f, float.MaxValue);

            var squad = new FakeSquad(10);
            pair.TryTrigger(0, squad, stageModifiers: controller);
            Assert.AreEqual(12, squad.SquadCount);
        }

        [Test]
        public void GatePair_WithRemapGateDivision_ConvertsDivideToSubtract_AndClampsToFloorOfOne()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();
            var gateGo = Track(new GameObject("Gate"));
            var gate = gateGo.AddComponent<Gate>();
            var perk = CreatePerk("div_2", "÷2", new DivideSoldiersEffect(2));
            gate.Initialize(0, perk, pair);

            var controller = new StageModifierController();
            var noMult = new GateOperationRemapStageModifier("mutacao:no_multiplication", false, true);
            controller.Activate(noMult, 0f, float.MaxValue);

            var squad = new FakeSquad(10);
            pair.TryTrigger(0, squad, stageModifiers: controller);
            Assert.AreEqual(8, squad.SquadCount);

            var pairExcessiveGo = Track(new GameObject("GatePairExcessive"));
            var pairExcessive = pairExcessiveGo.AddComponent<GatePair>();
            var gateExcessiveGo = Track(new GameObject("GateExcessive"));
            var gateExcessive = gateExcessiveGo.AddComponent<Gate>();
            var perkExcessive = CreatePerk("div_5", "÷5", new DivideSoldiersEffect(5));
            gateExcessive.Initialize(0, perkExcessive, pairExcessive);

            var smallSquad = new FakeSquad(3);
            pairExcessive.TryTrigger(0, smallSquad, stageModifiers: controller);
            Assert.AreEqual(1, smallSquad.SquadCount);
        }

        [Test]
        public void HordeSpawner_WithArmoredAndFrenzy_ScalesEffectiveEnemyHealthAndSpeed()
        {
            var spawnerGo = Track(new GameObject("HordeSpawner"));
            var spawner = spawnerGo.AddComponent<HordeSpawner>();
            var controller = new StageModifierController();
            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            var frenzy = new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f);
            controller.Activate(armored, 0f, float.MaxValue);
            controller.Activate(frenzy, 0f, float.MaxValue);

            spawner.StageModifierController = controller;
            spawner.DefaultEnemyHealth = 100;
            spawner.DefaultEnemySpeed = 2.0f;

            Assert.AreEqual(150, spawner.EffectiveEnemyHealth);
            Assert.AreEqual(2.6f, spawner.EffectiveEnemySpeed, 1e-4f);
        }

        [Test]
        public void LevelDirector_WithLastSoldier_OverridesInitialSquadOnInitialization()
        {
            var directorGo = Track(new GameObject("LevelDirector"));
            var director = directorGo.AddComponent<LevelDirector>();
            var controller = new StageModifierController();
            var lastSoldier = new InitialSquadOverrideStageModifier("mutacao:last_soldier", 1);
            controller.Activate(lastSoldier, 0f, float.MaxValue);

            var runConfig = new RunConfig("stage_01", initialSquad: 15, "pistol", 1f, null);
            director.Initialize(runConfig, null, null, null, controller);

            Assert.AreEqual(1, director.CurrentSquad);
        }

        [Test]
        public void LevelDirector_SpawnBossSegment_ScalesBossMaxHpByEnemyHealthMultiplier()
        {
            var directorGo = Track(new GameObject("LevelDirectorBoss"));
            var director = directorGo.AddComponent<LevelDirector>();
            var bus = new EventBus();
            var controller = new StageModifierController();
            var armored = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            controller.Activate(armored, 0f, float.MaxValue);

            var levelDef = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            levelDef.LevelId = "level_05_boss";
            levelDef.TotalDistance = 800f;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Boss,
                    StartDistance = 200f,
                    Length = 500f,
                    Boss = new BossSegmentSpec
                    {
                        BossId = "boss_colossus",
                        CustomMaxHp = 1000,
                        InitialLane = 0,
                        ArenaLength = 500f
                    }
                }
            };

            BossSpawnedEvent spawnedBossEvent = null;
            bus.Subscribe<BossSpawnedEvent>(evt => spawnedBossEvent = evt);

            director.Initialize(new RunConfig("level_05_boss", 10, "pistol", 1f, null), levelDef, bus, null, controller);
            director.Tick(200f);

            Assert.IsTrue(director.IsBossFightActive);
            Assert.IsNotNull(spawnedBossEvent);
            Assert.AreEqual(1500, spawnedBossEvent.MaxHp);
            Assert.AreEqual(1500, director.BossCombatState.CurrentHp);
            Assert.AreEqual(1500, director.BossCombatState.MaxHp);
        }
    }
}