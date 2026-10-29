using System;
using Game.Core.Gates;
using Game.Core.Modifiers;
using Game.Core.State;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class MutationGameplayModifiersTests
    {
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

            // Simulation of Multiply gate x2: with RemapGateMultiplication, becomes +2
            int squad = 10;
            int multFactor = 2;
            int squadAfterMultiply = controller.RemapGateMultiplication ? squad + multFactor : squad * multFactor;
            Assert.AreEqual(12, squadAfterMultiply);

            // Simulation of Multiply gate x3: becomes +3
            int squadAfterMult3 = controller.RemapGateMultiplication ? 5 + 3 : 5 * 3;
            Assert.AreEqual(8, squadAfterMult3);

            // Simulation of Divide gate /2: with RemapGateDivision, becomes -2
            int divFactor = 2;
            int squadAfterDivide = controller.RemapGateDivision ? Math.Max(1, squad - divFactor) : Math.Max(1, squad / divFactor);
            Assert.AreEqual(8, squadAfterDivide);

            // Simulation of Divide gate /5 on small squad: 3 - 5 = -2, clamped to minimum floor of 1 soldier
            int smallSquad = 3;
            int largeDivFactor = 5;
            int squadAfterExcessiveDiv = controller.RemapGateDivision
                ? Math.Max(1, smallSquad - largeDivFactor)
                : Math.Max(1, smallSquad / largeDivFactor);
            Assert.AreEqual(1, squadAfterExcessiveDiv);
        }
    }
}
