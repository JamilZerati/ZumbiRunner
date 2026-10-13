using System.Collections.Generic;
using System.Linq;
using Game.Core.Gates;
using Game.Data;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class LivingGatesSimulationAndValidationTests
    {
        private List<ScriptableObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
                if (_created[i] != null) Object.DestroyImmediate(_created[i]);
            _created.Clear();
        }

        private T Track<T>(T obj) where T : ScriptableObject { _created.Add(obj); return obj; }

        private GateDefinition CreateGate(string id, string name, string perkId, PerkDefinition perk, IGateVariant variant)
        {
            var gate = Track(ScriptableObject.CreateInstance<GateDefinition>());
            gate.SetData(id, name, perkId, perk, variant);
            return gate;
        }

        [Test]
        public void ValidateGates_CanonicalGatesFolder_ReportsZeroErrors()
        {
            int errors = ValidateCommand.ValidateGates("Assets/_Game/Data/Gates", out int count);
            Assert.AreEqual(0, errors);
            Assert.GreaterOrEqual(count, 4);
        }

        [Test]
        public void ValidateGate_NullOrMissingFields_ReturnsErrors()
        {
            var nullErrors = ValidateCommand.ValidateGate(null);
            Assert.AreEqual(1, nullErrors.Count);

            var emptyGate = CreateGate(string.Empty, string.Empty, string.Empty, null, null);
            var errors = ValidateCommand.ValidateGate(emptyGate);
            Assert.IsTrue(errors.Any(e => e.Contains("Id")) && errors.Any(e => e.Contains("Variant")));
        }

        [Test]
        public void ValidateGate_InvalidVariants_ReportsInvariants()
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData("p", "P", null);

            var upGate = CreateGate("g1", "+1", "p", perk, new ShootToUpgradeGateVariant(-5, 0, 0, 15));
            Assert.IsTrue(ValidateCommand.ValidateGate(upGate).Count > 0);

            var cursedGate = CreateGate("g2", "x3", "p", perk, new CursedGateVariant("p", new StageModifierSpec("c", 1.2f, 1f), 0f));
            Assert.IsTrue(ValidateCommand.ValidateGate(cursedGate).Count > 0);

            var guardedGate = CreateGate("g3", "x2", "p", perk, new GuardedGateVariant("w", "runner", 0, 8f));
            Assert.IsTrue(ValidateCommand.ValidateGate(guardedGate).Count > 0);
        }

        private LevelDefinition CreateLevelWithGate(string eventData, int initialTroops = 10)
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_sim_test";
            level.TotalDistance = 100f;
            level.Speed = 8f;
            level.InitialTroops = initialTroops;
            level.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Gate,
                    StartDistance = 10f,
                    Length = 20f,
                    Events = new[] { new SegmentEvent { Type = "Gate", Data = eventData } }
                }
            };
            return level;
        }

        [Test]
        public void Simulate_ShootToUpgrade_GulosoEvolvesValue_FujaoRespectsFloor()
        {
            var level = CreateLevelWithGate("gateId=gate_upgrade_minus5_to_15");
            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.IsTrue(guloso.victory);
            Assert.AreEqual(25, guloso.finalSquad);
            Assert.IsTrue(fujao.victory);
            Assert.AreEqual(7, fujao.finalSquad);
        }

        [Test]
        public void Simulate_CursedGate_GulosoTriplesSquad_FujaoAvoidsCurse()
        {
            var level = CreateLevelWithGate("gateId=gate_cursed_triple_frenzy");
            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.IsTrue(guloso.victory && guloso.finalSquad == 30);
            Assert.IsTrue(fujao.victory && fujao.finalSquad == 11);
        }

        [Test]
        public void Simulate_GuardedGate_GulosoFightsGuards_FujaoAvoidsCombat()
        {
            var level = CreateLevelWithGate("gateId=gate_guarded_multiply_2", initialTroops: 20);
            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.IsTrue(guloso.victory && guloso.kills > 0 && guloso.finalSquad == 40);
            Assert.IsTrue(fujao.victory && fujao.kills == 0 && fujao.finalSquad == 21);
        }

        [Test]
        public void Simulate_RunSimulation_CanonicalLevel01_CompletesWithoutExceptions()
        {
            var gSummary = SimulateCommand.RunSimulation("level_01", BotType.Guloso, 2, 42);
            var fSummary = SimulateCommand.RunSimulation("level_01", BotType.Fujao, 2, 42);
            Assert.IsNotNull(gSummary);
            Assert.IsNotNull(fSummary);
        }
    }
}
