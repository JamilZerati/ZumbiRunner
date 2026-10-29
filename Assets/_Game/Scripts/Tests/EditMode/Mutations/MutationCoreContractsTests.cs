using System.Collections.Generic;
using Game.Core.Gates;
using Game.Core.Modifiers;
using Game.Data.Mutations;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class MutationCoreContractsTests
    {
        [Test]
        public void CT_1_MutationCatalog_ContractAndEntries_ContainsFiveCanonicalMutationsWithExactData()
        {
            var catalog = ScriptableObject.CreateInstance<MutationCatalog>();

            var armored = ScriptableObject.CreateInstance<MutationDefinition>();
            armored.SetData(
                id: "armored",
                displayName: "Blindados",
                description: "Zumbis e chefes possuem +50% de HP base.",
                rewardBonus: 0.50f,
                modifiers: new IStageModifier[]
                {
                    new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f)
                });

            var frenzy = ScriptableObject.CreateInstance<MutationDefinition>();
            frenzy.SetData(
                id: "frenzy",
                displayName: "Frenesi",
                description: "Zumbis movem-se 30% mais rapido.",
                rewardBonus: 0.50f,
                modifiers: new IStageModifier[]
                {
                    new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f)
                });

            var fog = ScriptableObject.CreateInstance<MutationDefinition>();
            fog.SetData(
                id: "fog",
                displayName: "Neblina",
                description: "Alcance de tiro e engajamento da tropa reduzido em 40%.",
                rewardBonus: 0.25f,
                modifiers: new IStageModifier[]
                {
                    new SquadStatStageModifier("mutacao:fog", StatTarget.Range, 0.6f)
                });

            var lastSoldier = ScriptableObject.CreateInstance<MutationDefinition>();
            lastSoldier.SetData(
                id: "last_soldier",
                displayName: "Ultimo Soldado",
                description: "A tropa inicia a corrida com apenas 1 soldado.",
                rewardBonus: 1.00f,
                modifiers: new IStageModifier[]
                {
                    new InitialSquadOverrideStageModifier("mutacao:last_soldier", 1)
                });

            var noMultiplication = ScriptableObject.CreateInstance<MutationDefinition>();
            noMultiplication.SetData(
                id: "no_multiplication",
                displayName: "Sem Multiplicacao",
                description: "Portoes de multiplicacao viram adicao e divisao vira subtracao.",
                rewardBonus: 0.50f,
                modifiers: new IStageModifier[]
                {
                    new GateOperationRemapStageModifier("mutacao:no_multiplication", remapMultiplyToAdd: true, remapDivideToSubtract: true)
                });

            catalog.SetEntries(new[] { armored, frenzy, fog, lastSoldier, noMultiplication });

            Assert.AreEqual(5, catalog.Entries.Count);
            Assert.AreEqual(5, catalog.Mutations.Count);

            Assert.IsTrue(catalog.TryGet("armored", out var defArmored));
            Assert.AreEqual("Blindados", defArmored.DisplayName);
            Assert.AreEqual(0.50f, defArmored.RewardBonus, 1e-4f);
            Assert.AreEqual(1.5f, defArmored.Modifiers[0].EnemyHealthMultiplier, 1e-4f);
            Assert.IsNull(defArmored.Icon);

            Assert.IsTrue(catalog.TryGetMutation("armored", out var defArmoredMut));
            Assert.AreSame(defArmored, defArmoredMut);
            Assert.AreSame(defArmored, catalog.GetMutation("armored"));

            Assert.IsTrue(catalog.TryGet("frenzy", out var defFrenzy));
            Assert.AreEqual("Frenesi", defFrenzy.DisplayName);
            Assert.AreEqual(0.50f, defFrenzy.RewardBonus, 1e-4f);
            Assert.AreEqual(1.3f, defFrenzy.Modifiers[0].EnemySpeedMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGet("fog", out var defFog));
            Assert.AreEqual("Neblina", defFog.DisplayName);
            Assert.AreEqual(0.25f, defFog.RewardBonus, 1e-4f);
            Assert.AreEqual(0.6f, defFog.Modifiers[0].SquadRangeMultiplier, 1e-4f);

            Assert.IsTrue(catalog.TryGet("last_soldier", out var defLastSoldier));
            Assert.AreEqual(1.00f, defLastSoldier.RewardBonus, 1e-4f);
            Assert.AreEqual(1, defLastSoldier.Modifiers[0].InitialSquadOverride);

            Assert.IsTrue(catalog.TryGet("no_multiplication", out var defNoMult));
            Assert.AreEqual(0.50f, defNoMult.RewardBonus, 1e-4f);
            Assert.IsTrue(defNoMult.Modifiers[0].RemapGateMultiplication);
            Assert.IsTrue(defNoMult.Modifiers[0].RemapGateDivision);

            var bonuses = catalog.GetRewardBonuses();
            Assert.AreEqual(5, bonuses.Count);
            Assert.AreEqual(0.50f, bonuses["armored"], 1e-4f);
            Assert.AreEqual(0.50f, bonuses["frenzy"], 1e-4f);
            Assert.AreEqual(0.25f, bonuses["fog"], 1e-4f);
            Assert.AreEqual(1.00f, bonuses["last_soldier"], 1e-4f);
            Assert.AreEqual(0.50f, bonuses["no_multiplication"], 1e-4f);

            Assert.IsFalse(catalog.TryGet("unknown_mutation", out _));
            Assert.IsFalse(catalog.TryGetMutation("unknown_mutation", out _));
            Assert.IsNull(catalog.GetMutation("unknown_mutation"));
        }

        [Test]
        public void CT_2_StageModifierController_CalculatesCompositeMultipliers_ForHealthSpeedAndRange()
        {
            var controller = new StageModifierController();

            Assert.AreEqual(1.0f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, controller.CurrentEnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, controller.CurrentSquadRangeMultiplier, 1e-4f);
            Assert.IsNull(controller.CurrentInitialSquadOverride);
            Assert.IsFalse(controller.RemapGateMultiplication);
            Assert.IsFalse(controller.RemapGateDivision);

            var armoredMod = new EnemyStatStageModifier("mutacao:armored", StatTarget.MaxHealth, 1.5f);
            var frenzyMod = new EnemyStatStageModifier("mutacao:frenzy", StatTarget.MoveSpeed, 1.3f);
            var fogMod = new SquadStatStageModifier("mutacao:fog", StatTarget.Range, 0.6f);

            controller.Activate(armoredMod, startDistance: 0f, durationDistance: float.MaxValue);
            controller.Activate(frenzyMod, startDistance: 0f, durationDistance: float.MaxValue);
            controller.Activate(fogMod, startDistance: 0f, durationDistance: float.MaxValue);

            Assert.AreEqual(3, controller.ActiveCount);
            Assert.AreEqual(1.5f, controller.CurrentEnemyHealthMultiplier, 1e-4f);
            Assert.AreEqual(1.3f, controller.CurrentEnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(0.6f, controller.CurrentSquadRangeMultiplier, 1e-4f);
        }

        [Test]
        public void CoreModifiers_DefaultValues_AreNeutral()
        {
            var enemyMod = new EnemyStatStageModifier();
            Assert.AreEqual(1.0f, enemyMod.EnemySpeedMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, enemyMod.EnemyContactDamageMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, enemyMod.SquadRangeMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, enemyMod.EnemyHealthMultiplier, 1e-4f);
            Assert.IsNull(enemyMod.InitialSquadOverride);
            Assert.IsFalse(enemyMod.RemapGateMultiplication);
            Assert.IsFalse(enemyMod.RemapGateDivision);

            var squadMod = new SquadStatStageModifier();
            Assert.AreEqual(1.0f, squadMod.SquadRangeMultiplier, 1e-4f);

            var gateMod = new GateOperationRemapStageModifier();
            Assert.IsFalse(gateMod.RemapGateMultiplication);
            Assert.IsFalse(gateMod.RemapGateDivision);
        }
    }
}
