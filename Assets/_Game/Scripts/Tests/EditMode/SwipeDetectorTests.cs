using Game.Core;
using Game.Infrastructure.Input;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SwipeDetectorTests
    {
        private const float MinDistance = 40.0f;

        [Test]
        public void TryDetectSwipe_RightSwipe_ReturnsTrueAndDirectionPositive()
        {
            bool detected = SwipeDetector.TryDetectSwipe(100f, 200f, 160f, 205f, MinDistance, out int direction);

            Assert.IsTrue(detected);
            Assert.AreEqual(1, direction);
        }

        [Test]
        public void TryDetectSwipe_LeftSwipe_ReturnsTrueAndDirectionNegative()
        {
            bool detected = SwipeDetector.TryDetectSwipe(200f, 200f, 140f, 195f, MinDistance, out int direction);

            Assert.IsTrue(detected);
            Assert.AreEqual(-1, direction);
        }

        [Test]
        public void TryDetectSwipe_DistanceBelowThreshold_ReturnsFalse()
        {
            bool detected = SwipeDetector.TryDetectSwipe(100f, 200f, 120f, 200f, MinDistance, out int direction);

            Assert.IsFalse(detected);
            Assert.AreEqual(0, direction);
        }

        [Test]
        public void TryDetectSwipe_VerticalSwipe_ReturnsFalse()
        {
            bool detected = SwipeDetector.TryDetectSwipe(100f, 100f, 120f, 200f, MinDistance, out int direction);

            Assert.IsFalse(detected);
            Assert.AreEqual(0, direction);
        }

        [Test]
        public void TryDetectSwipe_DiagonalWithEqualDeltas_ReturnsFalse()
        {
            bool detected = SwipeDetector.TryDetectSwipe(100f, 100f, 150f, 150f, MinDistance, out int direction);

            Assert.IsFalse(detected);
            Assert.AreEqual(0, direction);
        }

        [Test]
        public void StandaloneLaneInput_TriggerMove_FiresMoveRequestedEvent()
        {
            var go = new GameObject("TestInput");
            try
            {
                var input = go.AddComponent<StandaloneLaneInput>();
                int receivedDirection = 0;
                input.MoveRequested += dir => receivedDirection = dir;

                input.TriggerMove(-1);
                Assert.AreEqual(-1, receivedDirection);

                input.TriggerMove(1);
                Assert.AreEqual(1, receivedDirection);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
