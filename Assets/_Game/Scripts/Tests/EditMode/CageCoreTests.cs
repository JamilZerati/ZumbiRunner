using System;
using Game.Core.Events;
using Game.Core.Rescue;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class CageCoreTests
    {
        [Test]
        public void InitialState_ValidParameters_SetsPropertiesCorrectly()
        {
            var state = new CageRuntimeState(cageId: "cage_01", laneIndex: 1, maxHp: 60, survivorCount: 3, lostEnemyArchetypeId: "walker");

            Assert.AreEqual("cage_01", state.CageId);
            Assert.AreEqual(1, state.LaneIndex);
            Assert.AreEqual(60, state.MaxHp);
            Assert.AreEqual(60, state.CurrentHp);
            Assert.AreEqual(3, state.SurvivorCount);
            Assert.AreEqual("walker", state.LostEnemyArchetypeId);
            Assert.IsFalse(state.IsOpen);
            Assert.IsFalse(state.IsLost);
        }

        [Test]
        public void InitialState_InvalidOrBoundaryParameters_ClampsToSafeDefaults()
        {
            var state = new CageRuntimeState(cageId: null, laneIndex: 0, maxHp: 0, survivorCount: -5, lostEnemyArchetypeId: null);

            Assert.AreEqual(string.Empty, state.CageId);
            Assert.AreEqual(1, state.MaxHp);
            Assert.AreEqual(1, state.CurrentHp);
            Assert.AreEqual(1, state.SurvivorCount);
            Assert.AreEqual("walker", state.LostEnemyArchetypeId);
        }

        [Test]
        public void TakeDamage_PartialDamage_ReducesCurrentHpWithoutOpening()
        {
            var state = new CageRuntimeState("cage_01", 0, 60, 3);

            bool opened = state.TakeDamage(25);

            Assert.IsFalse(opened);
            Assert.AreEqual(35, state.CurrentHp);
            Assert.IsFalse(state.IsOpen);
        }

        [Test]
        public void TakeDamage_LethalAndOverkillDamage_OpensCageAndClampsToZero()
        {
            var state = new CageRuntimeState("cage_01", 0, 60, 3);

            bool openedOnPartial = state.TakeDamage(50);
            Assert.IsFalse(openedOnPartial);
            Assert.AreEqual(10, state.CurrentHp);

            bool openedOnLethal = state.TakeDamage(20);
            Assert.IsTrue(openedOnLethal);
            Assert.AreEqual(0, state.CurrentHp);
            Assert.IsTrue(state.IsOpen);

            bool openedOnSubsequent = state.TakeDamage(10);
            Assert.IsFalse(openedOnSubsequent);
            Assert.AreEqual(0, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_ZeroOrNegativeDamage_Ignored()
        {
            var state = new CageRuntimeState("cage_01", 0, 50, 2);

            Assert.IsFalse(state.TakeDamage(0));
            Assert.AreEqual(50, state.CurrentHp);

            Assert.IsFalse(state.TakeDamage(-10));
            Assert.AreEqual(50, state.CurrentHp);
        }

        [Test]
        public void MarkLost_IntactCage_TransitionsToLostAndRejectsFurtherDamage()
        {
            var state = new CageRuntimeState("cage_01", 1, 40, 2);

            bool marked = state.MarkLost();

            Assert.IsTrue(marked);
            Assert.IsTrue(state.IsLost);
            Assert.IsFalse(state.IsOpen);

            Assert.IsFalse(state.MarkLost());
            Assert.IsFalse(state.TakeDamage(20));
            Assert.AreEqual(40, state.CurrentHp);
        }

        [Test]
        public void MarkLost_AlreadyOpenCage_CannotBeMarkedLost()
        {
            var state = new CageRuntimeState("cage_01", 0, 20, 1);
            state.TakeDamage(20);
            Assert.IsTrue(state.IsOpen);

            bool marked = state.MarkLost();

            Assert.IsFalse(marked);
            Assert.IsFalse(state.IsLost);
        }

        [Test]
        public void SurvivorsEvents_ConstructorsAndProperties_PreserveData()
        {
            var rescuedEvt = new SurvivorsRescuedEvent(count: 4, laneIndex: 2, cageId: "cage_heavy");
            Assert.AreEqual(4, rescuedEvt.Count);
            Assert.AreEqual(2, rescuedEvt.LaneIndex);
            Assert.AreEqual("cage_heavy", rescuedEvt.CageId);

            var lostEvt = new SurvivorsLostEvent(count: 2, laneIndex: 0, cageId: "cage_lost");
            Assert.AreEqual(2, lostEvt.Count);
            Assert.AreEqual(0, lostEvt.LaneIndex);
            Assert.AreEqual("cage_lost", lostEvt.CageId);
        }

        [Test]
        public void RunResult_RescuedProperty_DefaultsToZeroAndCanBeSet()
        {
            var result = new Game.Core.State.RunResult();
            Assert.AreEqual(0, result.Rescued);

            result.Rescued = 7;
            Assert.AreEqual(7, result.Rescued);
        }

        [Test]
        public void TakeDamage_StepByStepUntilZero_ReturnsTrueOnlyOnFinalHit()
        {
            var state = new CageRuntimeState("cage_gradual", 0, 30, 2);

            Assert.IsFalse(state.TakeDamage(10));
            Assert.AreEqual(20, state.CurrentHp);
            Assert.IsFalse(state.IsOpen);

            Assert.IsFalse(state.TakeDamage(10));
            Assert.AreEqual(10, state.CurrentHp);
            Assert.IsFalse(state.IsOpen);

            Assert.IsTrue(state.TakeDamage(10));
            Assert.AreEqual(0, state.CurrentHp);
            Assert.IsTrue(state.IsOpen);
        }

        [Test]
        public void State_IsIntact_FollowsLifecycleTransitions()
        {
            var state = new CageRuntimeState("cage_01", 0, 40, 2);
            Assert.IsTrue(state.IsIntact);

            state.TakeDamage(10);
            Assert.IsTrue(state.IsIntact);

            state.TakeDamage(30);
            Assert.IsFalse(state.IsIntact);
            Assert.IsTrue(state.IsOpen);

            var lostState = new CageRuntimeState("cage_02", 0, 40, 2);
            Assert.IsTrue(lostState.IsIntact);
            lostState.MarkLost();
            Assert.IsFalse(lostState.IsIntact);
            Assert.IsTrue(lostState.IsLost);
        }
    }
}
