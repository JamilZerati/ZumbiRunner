using System.Collections.Generic;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Data;
using Game.Editor.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class AbilitySimulationAndValidationTests
    {
        private readonly List<ScriptableObject> _createdAssets = new List<ScriptableObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdAssets.Count - 1; i >= 0; i--)
            {
                if (_createdAssets[i] != null)
                {
                    Object.DestroyImmediate(_createdAssets[i]);
                }
            }
            _createdAssets.Clear();
        }

        private GeneralAbilityDefinition CreateTrackedAbility(string id, string displayName, int chargeKills, IAbilityEffect effect)
        {
            var def = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            def.SetData(id, displayName, "Desc", chargeKills, effect, null);
            _createdAssets.Add(def);
            return def;
        }

        [Test]
        public void ValidateAbility_DefinicaoValida_RetornaZeroErros()
        {
            var ability = CreateTrackedAbility("grenade_test", "Granada", 25, new GrenadeAbilityEffect());
            var errors = ValidateCommand.ValidateAbility(ability);

            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void ValidateAbility_SemIdOuDisplayName_ReportaErros()
        {
            var ability = CreateTrackedAbility("", "", 25, new GrenadeAbilityEffect());
            var errors = ValidateCommand.ValidateAbility(ability);

            Assert.IsTrue(errors.Exists(e => e.Contains("Id")));
            Assert.IsTrue(errors.Exists(e => e.Contains("DisplayName")));
        }

        [Test]
        public void ValidateAbility_ChargeKillsZeroOuNegativo_ReportaErro()
        {
            var ability = CreateTrackedAbility("test", "Test", 0, new GrenadeAbilityEffect());
            var errors = ValidateCommand.ValidateAbility(ability);

            Assert.IsTrue(errors.Exists(e => e.Contains("ChargeKills")));
        }

        [Test]
        public void ValidateAbility_SemEffect_ReportaErro()
        {
            var ability = CreateTrackedAbility("test", "Test", 25, null);
            var errors = ValidateCommand.ValidateAbility(ability);

            Assert.IsTrue(errors.Exists(e => e.Contains("Effect")));
        }

        [Test]
        public void ValidateCommand_GrenadeAssetCanonico_PassaValido()
        {
            var grenade = AssetDatabase.LoadAssetAtPath<GeneralAbilityDefinition>("Assets/_Game/Data/Abilities/grenade.asset");
            Assert.IsNotNull(grenade, "grenade.asset deve existir no projeto.");

            var errors = ValidateCommand.ValidateAbility(grenade);
            Assert.AreEqual(0, errors.Count, "grenade.asset deve ser valido.");
        }

        [Test]
        public void ValidateCommand_Run_RetornaSucessoNoProjeto()
        {
            int exitCode = ValidateCommand.Run();
            Assert.AreEqual(0, exitCode);
        }

        [Test]
        public void SimulateCommand_SimulateSingleRun_ExecutaComSucessoEAcumulaAbates()
        {
            var level = SimulateCommand.LoadLevel("level_01");
            Assert.IsNotNull(level, "level_01 deve ser carregado com sucesso.");

            var result = SimulateCommand.SimulateSingleRun(level, BotType.Guloso, 42, 0);

            Assert.IsNotNull(result);
            Assert.Greater(result.DistanceReached, 0f);
            Assert.GreaterOrEqual(result.Kills, 0);
        }
    }
}
