using NUnit.Framework;
using Game.Core;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class SquadHealthBufferTests
    {
        [Test]
        public void DamageLessThanSoldierHealth_DoesNotKillSoldier()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lost = buffer.ApplyDamage(4f, currentSquadCount: 5, out bool generalDied);

            Assert.AreEqual(0, lost);
            Assert.AreEqual(6f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.IsFalse(generalDied);
        }

        [Test]
        public void CumulativeDamage_KillsSoldier_AndResetsBufferForNextSoldier()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            buffer.ApplyDamage(6f, currentSquadCount: 3, out _);
            int lost = buffer.ApplyDamage(7f, currentSquadCount: 3, out bool generalDied);

            Assert.AreEqual(1, lost);
            Assert.AreEqual(7f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.IsFalse(generalDied);
        }

        [Test]
        public void MassiveDamage_KillsMultipleSoldiers()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lost = buffer.ApplyDamage(25f, currentSquadCount: 5, out bool generalDied);

            Assert.AreEqual(2, lost);
            Assert.AreEqual(5f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.IsFalse(generalDied);
        }

        [Test]
        public void DamageOverflowsToGeneral_WhenSquadReachesZero()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lost = buffer.ApplyDamage(15f, currentSquadCount: 1, out bool generalDied);

            Assert.AreEqual(1, lost);
            Assert.AreEqual(0f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.AreEqual(5f, buffer.CurrentGeneralHealth, 0.001f);
            Assert.IsFalse(generalDied);
        }

        [Test]
        public void LethalDamageToGeneral_TriggersGeneralDied()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lost = buffer.ApplyDamage(25f, currentSquadCount: 1, out bool generalDied);

            Assert.AreEqual(1, lost);
            Assert.AreEqual(0f, buffer.CurrentGeneralHealth, 0.001f);
            Assert.IsTrue(generalDied);
        }

        [Test]
        public void DamageToGeneralDirectly_WhenSquadIsAlreadyZero()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lost = buffer.ApplyDamage(4f, currentSquadCount: 0, out bool generalDied);

            Assert.AreEqual(0, lost);
            Assert.AreEqual(6f, buffer.CurrentGeneralHealth, 0.001f);
            Assert.AreEqual(10f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.IsFalse(generalDied);
            Assert.IsTrue(buffer.IsGeneralAlive);
        }

        [Test]
        public void DamageLessThanOrEqualToZero_ReturnsZero_AndNoStateChange()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            int lostZero = buffer.ApplyDamage(0f, currentSquadCount: 3, out bool generalDiedZero);
            Assert.AreEqual(0, lostZero);
            Assert.AreEqual(10f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.AreEqual(10f, buffer.CurrentGeneralHealth, 0.001f);
            Assert.IsFalse(generalDiedZero);

            int lostNeg = buffer.ApplyDamage(-5f, currentSquadCount: 3, out bool generalDiedNeg);
            Assert.AreEqual(0, lostNeg);
            Assert.AreEqual(10f, buffer.CurrentSoldierHealth, 0.001f);
            Assert.AreEqual(10f, buffer.CurrentGeneralHealth, 0.001f);
            Assert.IsFalse(generalDiedNeg);
        }

        [Test]
        public void ResetSoldierHealth_And_ResetGeneralHealth()
        {
            var buffer = new SquadHealthBuffer(10f, 10f);
            buffer.ApplyDamage(4f, currentSquadCount: 2, out _);
            Assert.AreEqual(6f, buffer.CurrentSoldierHealth, 0.001f);

            buffer.ResetSoldierHealth();
            Assert.AreEqual(10f, buffer.CurrentSoldierHealth, 0.001f);

            buffer.ApplyDamage(4f, currentSquadCount: 0, out _);
            Assert.AreEqual(6f, buffer.CurrentGeneralHealth, 0.001f);

            buffer.ResetGeneralHealth();
            Assert.AreEqual(10f, buffer.CurrentGeneralHealth, 0.001f);
        }
    }
}
