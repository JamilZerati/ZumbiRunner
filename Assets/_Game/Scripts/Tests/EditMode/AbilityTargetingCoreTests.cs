using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class AbilityTargetingCoreTests
    {
        [Test]
        public void InstantTargeting_ClampsToOrigin()
        {
            var config = new AbilityTargetingConfig { Type = AbilityTargetingType.Instant, MaxRange = 0f };
            var origin = new AbilityPosition(0f, 0f, 10f);
            var desired = new AbilityPosition(2f, 0f, 25f);

            var result = config.ClampTarget(origin, desired);

            Assert.AreEqual(origin.X, result.X);
            Assert.AreEqual(origin.Z, result.Z);
        }

        [Test]
        public void GroundTarget_ClampsZDistanceToMaxRange()
        {
            var config = new AbilityTargetingConfig { Type = AbilityTargetingType.GroundTarget, MaxRange = 20f };
            var origin = new AbilityPosition(0f, 0f, 10f);
            var desiredFar = new AbilityPosition(1f, 0f, 40f);

            var result = config.ClampTarget(origin, desiredFar);

            Assert.AreEqual(1f, result.X);
            Assert.AreEqual(30f, result.Z); // origin.Z + MaxRange
        }

        [Test]
        public void GroundTarget_DoesNotAllowNegativeZBehindOrigin()
        {
            var config = new AbilityTargetingConfig { Type = AbilityTargetingType.GroundTarget, MaxRange = 20f };
            var origin = new AbilityPosition(0f, 0f, 10f);
            var desiredBehind = new AbilityPosition(1f, 0f, 5f);

            var result = config.ClampTarget(origin, desiredBehind);

            Assert.AreEqual(10f, result.Z); // clamped at origin.Z
        }
    }
}
