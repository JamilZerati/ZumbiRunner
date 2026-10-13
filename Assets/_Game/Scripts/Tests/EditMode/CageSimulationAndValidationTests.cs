using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class CageSimulationAndValidationTests
    {
        private readonly List<ScriptableObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }
            _created.Clear();
        }

        private T Track<T>(T obj) where T : ScriptableObject
        {
            _created.Add(obj);
            return obj;
        }

        private CageDefinition CreateCage(string id, string name, int hp, int survivors, string archetypeId = "walker")
        {
            var cage = Track(ScriptableObject.CreateInstance<CageDefinition>());
            cage.SetData(id, name, hp, survivors, archetypeId);
            return cage;
        }

        private LevelDefinition CreateLevelWithCage(string eventData = null, CageSpawnSpec[] cages = null, bool includeHorde = false, int initialTroops = 10)
        {
            var level = Track(ScriptableObject.CreateInstance<LevelDefinition>());
            level.LevelId = "level_cage_sim_test";
            level.TotalDistance = includeHorde ? 200f : 100f;
            level.Speed = 8f;
            level.InitialTroops = initialTroops;

            var segments = new List<SegmentDefinition>
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Cage,
                    StartDistance = 10f,
                    Length = 20f,
                    Cages = cages,
                    Events = string.IsNullOrEmpty(eventData) ? null : new[] { new SegmentEvent { Type = "Cage", Data = eventData, Lane = 0 } }
                }
            };

            if (includeHorde)
            {
                segments.Add(new SegmentDefinition
                {
                    SegmentType = SegmentType.Horde,
                    StartDistance = 30f,
                    Length = 70f,
                    Events = new[] { new SegmentEvent { Type = "HordeSpawn", Data = "count=5 archetype=walker lane=0", DistanceOffset = 10f, Lane = 0 } }
                });
            }

            level.Segments = segments.ToArray();
            return level;
        }

        [Test]
        public void ValidateCages_CanonicalCagesFolder_ReportsZeroErrors()
        {
            int errors = ValidateCommand.ValidateCages("Assets/_Game/Data/Cages", out int count);
            Assert.AreEqual(0, errors);
            Assert.GreaterOrEqual(count, 1);
        }

        [Test]
        public void ValidateCage_NullOrMissingFields_ReturnsErrors()
        {
            var nullErrors = ValidateCommand.ValidateCage(null);
            Assert.AreEqual(1, nullErrors.Count);

            var emptyCage = CreateCage(string.Empty, string.Empty, 0, 0, string.Empty);
            var errors = ValidateCommand.ValidateCage(emptyCage);
            Assert.IsTrue(errors.Any(e => e.Contains("Id")));
            Assert.IsTrue(errors.Any(e => e.Contains("DisplayName")));
            Assert.IsTrue(errors.Any(e => e.Contains("MaxHp")));
            Assert.IsTrue(errors.Any(e => e.Contains("SurvivorCount")));
            Assert.IsTrue(errors.Any(e => e.Contains("LostEnemyArchetypeId")));
        }

        [Test]
        public void ValidateCage_MissingEnemyArchetype_ReturnsError()
        {
            var invalidArchCage = CreateCage("cage_invalid", "Invalid Archetype Cage", 50, 2, "non_existent_archetype_xyz");
            var errors = ValidateCommand.ValidateCage(invalidArchCage);
            Assert.IsTrue(errors.Any(e => e.Contains("references missing enemy archetype")));
        }

        [Test]
        public void Simulate_Cage_GulosoRescuesSurvivorsWhenDamageSufficient()
        {
            var level = CreateLevelWithCage("cageId=cage_standard_3", initialTroops: 10);
            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);

            Assert.IsTrue(guloso.victory);
            Assert.AreEqual(3, guloso.rescued);
            Assert.AreEqual(13, guloso.finalSquad);
        }

        [Test]
        public void Simulate_Cage_FujaoBypassesCage_DoesNotRescueSurvivors()
        {
            var level = CreateLevelWithCage("cageId=cage_standard_3", initialTroops: 10);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.IsTrue(fujao.victory);
            Assert.AreEqual(0, fujao.rescued);
            Assert.AreEqual(10, fujao.finalSquad);
        }

        [Test]
        public void Simulate_Cage_GulosoInsufficientDamage_LeavesCageUnopened()
        {
            var level = CreateLevelWithCage("cageId=cage_standard_3 hp=5000 survivors=5", initialTroops: 1);
            var guloso = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);

            Assert.IsTrue(guloso.victory);
            Assert.AreEqual(0, guloso.rescued);
            Assert.AreEqual(1, guloso.finalSquad);
        }

        [Test]
        public void Simulate_Cage_FujaoBypassesCage_IncreasesSubsequentHordePenalty()
        {
            var level = CreateLevelWithCage("cageId=cage_standard_3 survivors=5", includeHorde: true, initialTroops: 2);
            var fujao = SimulateCommand.SimulateSingleRun(level, BotType.Fujao, 42, 0);

            Assert.AreEqual(0, fujao.rescued);
        }

        [Test]
        public void Simulate_RunSimulation_WithCageSegment_CalculatesAveragesAndTotals()
        {
            var level = CreateLevelWithCage("cageId=cage_standard_3", initialTroops: 10);
            var gSummary = SimulateCommand.RunSimulation(level, BotType.Guloso, 5, 42);
            var fSummary = SimulateCommand.RunSimulation(level, BotType.Fujao, 5, 42);

            Assert.IsNotNull(gSummary);
            Assert.AreEqual(15, gSummary.totalRescued);
            Assert.AreEqual(3f, gSummary.averageRescued);

            Assert.IsNotNull(fSummary);
            Assert.AreEqual(0, fSummary.totalRescued);
            Assert.AreEqual(0f, fSummary.averageRescued);
        }
    }
}
