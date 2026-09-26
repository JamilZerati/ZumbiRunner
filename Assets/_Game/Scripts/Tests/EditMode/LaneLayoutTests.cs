using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class LaneLayoutTests
    {
        [Test]
        public void LaneLayout_InitializesWithValidLaneCount()
        {
            var layout = new LaneLayout(2);

            Assert.AreEqual(2, layout.LaneCount);
            Assert.IsTrue(layout.IsValidLane(0));
            Assert.IsTrue(layout.IsValidLane(1));
            Assert.IsFalse(layout.IsValidLane(2));
            Assert.IsFalse(layout.IsValidLane(-1));
        }

        [Test]
        public void LaneLayout_ThrowsWhenLaneCountLessThanOne()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaneLayout(0));
        }
    }
}
