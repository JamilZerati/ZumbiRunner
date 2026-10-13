using System;
using Game.Core.Events;
using Game.Core.Holdout;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class HoldoutCoreTests
    {
        [Test]
        public void InitialState_ValidParameters_SetsPropertiesCorrectly()
        {
            var state = new HoldoutRuntimeState(
                holdoutId: "holdout_alpha",
                maxHp: 200,
                totalSurvivors: 4,
                durationSeconds: 15f);

            Assert.AreEqual("holdout_alpha", state.HoldoutId);
            Assert.AreEqual(200, state.MaxHp);
            Assert.AreEqual(200, state.CurrentHp);
            Assert.AreEqual(4, state.TotalSurvivors);
            Assert.AreEqual(0, state.SurvivorsSaved);
            Assert.AreEqual(15f, state.DurationSeconds);
            Assert.AreEqual(15f, state.TimeRemaining);
            Assert.AreEqual(HoldoutStatus.Defense, state.Status);
            Assert.IsTrue(state.IsActive);
            Assert.IsFalse(state.IsVictory);
            Assert.IsFalse(state.IsBarricadeDestroyed);
        }

        [Test]
        public void InitialState_InvalidOrBoundaryParameters_ClampsToSafeDefaults()
        {
            var state = new HoldoutRuntimeState(
                holdoutId: null,
                maxHp: -10,
                totalSurvivors: -5,
                durationSeconds: -2f);

            Assert.AreEqual(string.Empty, state.HoldoutId);
            Assert.AreEqual(1, state.MaxHp);
            Assert.AreEqual(1, state.CurrentHp);
            Assert.AreEqual(0, state.TotalSurvivors);
            Assert.AreEqual(0, state.SurvivorsSaved);
            Assert.AreEqual(0f, state.DurationSeconds);
            Assert.AreEqual(0f, state.TimeRemaining);
            Assert.AreEqual(HoldoutStatus.Defense, state.Status);
        }

        [Test]
        public void TakeDamage_PartialDamage_ReducesCurrentHpWithoutDestroying()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 5, 20f);

            bool destroyedOnFirstHit = state.TakeDamage(25);
            Assert.IsFalse(destroyedOnFirstHit);
            Assert.AreEqual(75, state.CurrentHp);
            Assert.IsTrue(state.IsActive);
            Assert.IsFalse(state.IsBarricadeDestroyed);

            bool destroyedOnSecondHit = state.TakeDamage(25);
            Assert.IsFalse(destroyedOnSecondHit);
            Assert.AreEqual(50, state.CurrentHp);
            Assert.IsTrue(state.IsActive);
        }

        [Test]
        public void TakeDamage_ZeroOrNegativeDamage_Ignored()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 3, 10f);

            Assert.IsFalse(state.TakeDamage(0));
            Assert.AreEqual(100, state.CurrentHp);

            Assert.IsFalse(state.TakeDamage(-15));
            Assert.AreEqual(100, state.CurrentHp);
        }

        [Test]
        public void TakeDamage_LethalAndOverkillDamage_TransitionsToDestroyedAndClampsToZero()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 4, 15f);

            state.TakeDamage(80);
            Assert.AreEqual(20, state.CurrentHp);
            Assert.IsFalse(state.IsBarricadeDestroyed);

            bool destroyed = state.TakeDamage(50);
            Assert.IsTrue(destroyed);
            Assert.AreEqual(0, state.CurrentHp);
            Assert.AreEqual(HoldoutStatus.BarricadeDestroyed, state.Status);
            Assert.IsTrue(state.IsBarricadeDestroyed);
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(0, state.SurvivorsSaved);

            bool destroyedAgain = state.TakeDamage(20);
            Assert.IsFalse(destroyedAgain);
            Assert.AreEqual(0, state.CurrentHp);
        }

        [Test]
        public void ConcludeHoldout_FullHpVictory_RescuesAllSurvivors()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 5, 10f);

            state.ConcludeHoldout(timeoutOrCleared: true);

            Assert.AreEqual(HoldoutStatus.Victory, state.Status);
            Assert.IsTrue(state.IsVictory);
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(5, state.SurvivorsSaved);
        }

        [Test]
        public void ConcludeHoldout_HalfHpVictory_RescuesProportionRoundedUp()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 5, 10f);
            state.TakeDamage(50);
            Assert.AreEqual(50, state.CurrentHp);

            state.ConcludeHoldout(timeoutOrCleared: true);

            Assert.AreEqual(HoldoutStatus.Victory, state.Status);
            Assert.AreEqual(3, state.SurvivorsSaved);
        }

        [Test]
        public void ConcludeHoldout_SlightHpVictory_RescuesAtLeastOneSurvivor()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 5, 10f);
            state.TakeDamage(99);
            Assert.AreEqual(1, state.CurrentHp);

            state.ConcludeHoldout(timeoutOrCleared: true);

            Assert.AreEqual(HoldoutStatus.Victory, state.Status);
            Assert.AreEqual(1, state.SurvivorsSaved);
        }

        [Test]
        public void ConcludeHoldout_Failure_DestroysBarricadeAndZeroesSurvivors()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 5, 10f);
            state.TakeDamage(30);

            state.ConcludeHoldout(timeoutOrCleared: false);

            Assert.AreEqual(HoldoutStatus.BarricadeDestroyed, state.Status);
            Assert.IsTrue(state.IsBarricadeDestroyed);
            Assert.AreEqual(0, state.SurvivorsSaved);
        }

        [Test]
        public void Tick_ElapsedTime_DecrementsTimeRemainingAndConcludesOnExpiry()
        {
            var state = new HoldoutRuntimeState("holdout_01", 80, 4, 10f);

            state.Tick(4f);
            Assert.AreEqual(6f, state.TimeRemaining);
            Assert.IsTrue(state.IsActive);

            state.Tick(6f);
            Assert.AreEqual(0f, state.TimeRemaining);
            Assert.AreEqual(HoldoutStatus.Victory, state.Status);
            Assert.IsTrue(state.IsVictory);
            Assert.AreEqual(4, state.SurvivorsSaved);
        }

        [Test]
        public void Tick_ZeroOrNegativeDeltaTime_Ignored()
        {
            var state = new HoldoutRuntimeState("holdout_01", 50, 2, 8f);

            state.Tick(0f);
            Assert.AreEqual(8f, state.TimeRemaining);

            state.Tick(-3f);
            Assert.AreEqual(8f, state.TimeRemaining);
        }

        [Test]
        public void StartDefense_CanResetOrRestartDefense()
        {
            var state = new HoldoutRuntimeState("holdout_01", 100, 3, 12f);
            state.TakeDamage(40);
            state.Tick(5f);

            state.StartDefense();

            Assert.AreEqual(100, state.CurrentHp);
            Assert.AreEqual(12f, state.TimeRemaining);
            Assert.AreEqual(HoldoutStatus.Defense, state.Status);
            Assert.AreEqual(0, state.SurvivorsSaved);
        }

        [Test]
        public void Events_HoldoutStartedAndEnded_PreserveData()
        {
            var started = new HoldoutStartedEvent("holdout_gate", 150, 6, 25f);
            Assert.AreEqual("holdout_gate", started.HoldoutId);
            Assert.AreEqual(150, started.MaxHp);
            Assert.AreEqual(6, started.TotalSurvivors);
            Assert.AreEqual(25f, started.DurationSeconds);

            var clampedStarted = new HoldoutStartedEvent(null, -10, -3, -5f);
            Assert.AreEqual(string.Empty, clampedStarted.HoldoutId);
            Assert.AreEqual(1, clampedStarted.MaxHp);
            Assert.AreEqual(0, clampedStarted.TotalSurvivors);
            Assert.AreEqual(0f, clampedStarted.DurationSeconds);

            var endedHeld = new HoldoutEndedEvent("holdout_gate", 5, 80, true);
            Assert.AreEqual("holdout_gate", endedHeld.HoldoutId);
            Assert.AreEqual(5, endedHeld.SurvivorsSaved);
            Assert.AreEqual(80, endedHeld.RemainingHp);
            Assert.IsTrue(endedHeld.BarricadeHeld);

            var clampedEnded = new HoldoutEndedEvent(null, -2, -10, false);
            Assert.AreEqual(string.Empty, clampedEnded.HoldoutId);
            Assert.AreEqual(0, clampedEnded.SurvivorsSaved);
            Assert.AreEqual(0, clampedEnded.RemainingHp);
            Assert.IsFalse(clampedEnded.BarricadeHeld);
        }
    }
}
