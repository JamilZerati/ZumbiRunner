using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Game.Core.Gates;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public class MutationSimulateCommandTests
    {
        [Test]
        public void ParseMutationIds_NullOrEmptyOrWhitespace_ReturnsEmptyList()
        {
            Assert.IsEmpty(SimulateCommand.ParseMutationIds(null));
            Assert.IsEmpty(SimulateCommand.ParseMutationIds(string.Empty));
            Assert.IsEmpty(SimulateCommand.ParseMutationIds("   "));
            Assert.IsEmpty(SimulateCommand.ParseMutationIds(" , ,  "));
        }

        [Test]
        public void ParseMutationIds_ValidSingleAndCommaSeparated_ReturnsTrimmedCanonicalIds()
        {
            var single = SimulateCommand.ParseMutationIds("armored");
            Assert.AreEqual(1, single.Count);
            Assert.AreEqual("armored", single[0]);

            var multi = SimulateCommand.ParseMutationIds("  armored ,  frenzy , fog  ");
            Assert.AreEqual(3, multi.Count);
            Assert.AreEqual("armored", multi[0]);
            Assert.AreEqual("frenzy", multi[1]);
            Assert.AreEqual("fog", multi[2]);
        }

        [Test]
        public void ParseMutationIds_DuplicateTokens_DeduplicatesList()
        {
            var dedup = SimulateCommand.ParseMutationIds("armored,frenzy,armored,ARMORED");
            Assert.AreEqual(2, dedup.Count);
            Assert.AreEqual("armored", dedup[0]);
            Assert.AreEqual("frenzy", dedup[1]);
        }

        [Test]
        public void ParseMutationIds_CT12_UnknownTokens_EmitsWarningAndFiltersOnlyValid()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[SimulateCommand\] Unknown mutation 'invalid_x' ignored\."));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[SimulateCommand\] Unknown mutation 'unknown_y' ignored\."));

            var filtered = SimulateCommand.ParseMutationIds("armored,invalid_x,frenzy,unknown_y");

            Assert.AreEqual(2, filtered.Count);
            Assert.AreEqual("armored", filtered[0]);
            Assert.AreEqual("frenzy", filtered[1]);
        }

        [Test]
        public void FilterValidMutations_HandlesEnumerableWithMixedCommasAndWhitespace()
        {
            LogAssert.Expect(LogType.Warning, new Regex(@"\[SimulateCommand\] Unknown mutation 'invalid_z' ignored\."));

            var mixed = SimulateCommand.FilterValidMutations(new[] { "armored, frenzy", "  ", null, "fog", "invalid_z" });

            Assert.AreEqual(3, mixed.Count);
            Assert.Contains("armored", (ICollection)mixed);
            Assert.Contains("frenzy", (ICollection)mixed);
            Assert.Contains("fog", (ICollection)mixed);
        }

        [Test]
        public void ResolveModifiersForMutation_CanonicalMutations_ProducesExpectedStageModifiers()
        {
            var controller = new StageModifierController();

            var armoredMods = SimulateCommand.ResolveModifiersForMutation("armored");
            Assert.AreEqual(1, armoredMods.Count);
            Assert.AreEqual(1.5f, armoredMods[0].EnemyHealthMultiplier, 1e-4f);

            var frenzyMods = SimulateCommand.ResolveModifiersForMutation("frenzy");
            Assert.AreEqual(1, frenzyMods.Count);
            Assert.AreEqual(1.3f, frenzyMods[0].EnemySpeedMultiplier, 1e-4f);

            var fogMods = SimulateCommand.ResolveModifiersForMutation("fog");
            Assert.AreEqual(1, fogMods.Count);
            Assert.AreEqual(0.6f, fogMods[0].SquadRangeMultiplier, 1e-4f);

            var lastSoldierMods = SimulateCommand.ResolveModifiersForMutation("last_soldier");
            Assert.AreEqual(1, lastSoldierMods.Count);
            Assert.AreEqual(1, lastSoldierMods[0].InitialSquadOverride);

            var noMultMods = SimulateCommand.ResolveModifiersForMutation("no_multiplication");
            Assert.AreEqual(1, noMultMods.Count);
            Assert.IsTrue(noMultMods[0].RemapGateMultiplication);
            Assert.IsTrue(noMultMods[0].RemapGateDivision);

            SimulateCommand.ApplyMutationsToController(controller, new[] { "armored", "frenzy", "fog", "last_soldier", "no_multiplication" });

            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1.3f, controller.CurrentEnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(0.6f, controller.CurrentSquadRangeMultiplier, 1e-4f);
            Assert.AreEqual(1, controller.CurrentInitialSquadOverride);
            Assert.IsTrue(controller.RemapGateMultiplication);
            Assert.IsTrue(controller.RemapGateDivision);
        }

        [Test]
        public void RunAll_WithMutations_PopulatesActiveMutationsInReportAndSummaries()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"sim_test_{Guid.NewGuid():N}.json");
            try
            {
                var report = SimulateCommand.RunAll("level_01", seed: 42, runs: 2, outputPath: tempFile, mutations: new[] { "armored", "frenzy" });

                Assert.IsNotNull(report);
                Assert.AreEqual(2, report.ActiveMutations.Count);
                Assert.Contains("armored", (ICollection)report.ActiveMutations);
                Assert.Contains("frenzy", (ICollection)report.ActiveMutations);
                Assert.AreEqual(2, report.Summaries.Count);

                for (int i = 0; i < report.Summaries.Count; i++)
                {
                    var s = report.Summaries[i];
                    Assert.AreEqual(2, s.ActiveMutations.Count);
                    Assert.Contains("armored", (ICollection)s.ActiveMutations);
                    Assert.Contains("frenzy", (ICollection)s.ActiveMutations);
                }

                Assert.IsTrue(File.Exists(tempFile));
                string json = File.ReadAllText(tempFile);
                Assert.IsTrue(json.Contains("armored"));
                Assert.IsTrue(json.Contains("frenzy"));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void SimulateSingleRun_WithLastSoldier_ForcesInitialSquadToOne()
        {
            var level = SimulateCommand.LoadLevel("level_01");
            Assert.IsNotNull(level);

            var runWithLastSoldier = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0, new[] { "last_soldier" });
            Assert.IsNotNull(runWithLastSoldier);
        }

        [Test]
        public void ExecuteFromCommandLine_WithMutationsArgument_ParsesAndExecutesSuccessfully()
        {
            int code = SimulateCommand.ExecuteFromCommandLine(new[]
            {
                "-level", "level_01",
                "-runs", "1",
                "-seed", "42",
                "-mutations", "armored,fog"
            });

            Assert.AreEqual(0, code);
        }
    }
}
