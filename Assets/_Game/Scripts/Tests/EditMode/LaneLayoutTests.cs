using System;
using Game.Core;
using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class LaneLayoutTests
    {
        [Test]
        public void LaneLayout_InitializesWithValidLaneCount()
        {
            var layout = new LaneLayout(2);

            Assert.AreEqual(2, layout.LaneCount);
            Assert.AreEqual(2.0f, layout.LaneWidth);
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

        [Test]
        public void LaneLayout_ThrowsWhenLaneWidthZeroOrNegative()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaneLayout(2, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LaneLayout(2, -1.5f));
        }

        [Test]
        public void GetLaneCenterX_CalculatesSymmetricCenters_ForTwoLanes()
        {
            var layout = new LaneLayout(2, 2.0f);

            Assert.AreEqual(-1.0f, layout.GetLaneCenterX(0), 1e-5f);
            Assert.AreEqual(1.0f, layout.GetLaneCenterX(1), 1e-5f);
        }

        [Test]
        public void GetLaneCenterX_CalculatesSymmetricCenters_ForThreeLanes()
        {
            var layout = new LaneLayout(3, 2.0f);

            Assert.AreEqual(-2.0f, layout.GetLaneCenterX(0), 1e-5f);
            Assert.AreEqual(0.0f, layout.GetLaneCenterX(1), 1e-5f);
            Assert.AreEqual(2.0f, layout.GetLaneCenterX(2), 1e-5f);
        }

        [Test]
        public void GetLaneCenterX_SingleLane_IsCenteredAtZero()
        {
            var layout = new LaneLayout(1, 3.0f);

            Assert.AreEqual(0.0f, layout.GetLaneCenterX(0), 1e-5f);
        }

        [Test]
        public void GetLaneCenterX_ThrowsWhenLaneIndexInvalid()
        {
            var layout = new LaneLayout(2, 2.0f);

            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLaneCenterX(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => layout.GetLaneCenterX(2));
        }

        [Test]
        public void LaneLayoutDefinition_ProvidesEquivalentLaneLayout()
        {
            var definition = ScriptableObject.CreateInstance<LaneLayoutDefinition>();

            Assert.AreEqual(2, definition.LaneCount);
            Assert.AreEqual(2.0f, definition.LaneWidth);
            Assert.AreEqual(-1.0f, definition.GetLaneCenterX(0), 1e-5f);
            Assert.AreEqual(1.0f, definition.GetLaneCenterX(1), 1e-5f);
        }
    }
}
