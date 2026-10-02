using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Game.Data;

namespace Game.Tests.EditMode
{
    public class LevelValidationTests
    {
        private List<ScriptableObject> _assetsToDestroy;

        [SetUp]
        public void SetUp()
        {
            _assetsToDestroy = new List<ScriptableObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_assetsToDestroy != null)
            {
                for (int i = 0; i < _assetsToDestroy.Count; i++)
                {
                    if (_assetsToDestroy[i] != null)
                    {
                        Object.DestroyImmediate(_assetsToDestroy[i]);
                    }
                }
                _assetsToDestroy.Clear();
            }
        }

        private T CreateScriptableObject<T>() where T : ScriptableObject
        {
            var so = ScriptableObject.CreateInstance<T>();
            _assetsToDestroy.Add(so);
            return so;
        }

        [Test]
        public void Validate_HordeSegment_OverlapUnder8m_ShouldReject()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "overlap_test";
            levelDef.TotalDistance = 200f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Gate,
                    StartDistance = 20f,
                    Length = 30f
                },
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Horde,
                    StartDistance = 45f,
                    Length = 30f,
                    CoveredLanes = new[] { 0, 1 }
                }
            };

            var invalidResult = LevelValidator.Validate(levelDef);
            Assert.IsFalse(invalidResult.IsValid);
            Assert.IsTrue(invalidResult.Errors.Any(e => e.Contains("8.0m") || e.Contains("overlap") || e.Contains("Overlap")));

            levelDef.Segments[1].StartDistance = 40f;
            var validResult = LevelValidator.Validate(levelDef);
            Assert.IsTrue(validResult.IsValid);
        }

        [Test]
        public void Validate_EmptyLane_ShouldReject()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "empty_lane_test";
            levelDef.TotalDistance = 200f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Horde,
                    StartDistance = 50f,
                    Length = 50f,
                    Events = new[]
                    {
                        new SegmentEvent
                        {
                            DistanceOffset = 10f,
                            Type = "HordeSpawn",
                            Lane = 0
                        }
                    }
                }
            };

            var invalidResult = LevelValidator.Validate(levelDef);
            Assert.IsFalse(invalidResult.IsValid);
            Assert.IsTrue(invalidResult.Errors.Any(e => e.Contains("lane") || e.Contains("Lane")));

            levelDef.Segments[0].Events = new[]
            {
                new SegmentEvent
                {
                    DistanceOffset = 10f,
                    Type = "HordeSpawn",
                    Lane = 0
                },
                new SegmentEvent
                {
                    DistanceOffset = 10f,
                    Type = "HordeSpawn",
                    Lane = 1
                }
            };

            var validResult = LevelValidator.Validate(levelDef);
            Assert.IsTrue(validResult.IsValid);
        }
    }
}
