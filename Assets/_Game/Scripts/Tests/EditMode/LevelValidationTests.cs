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

        [Test]
        public void Validate_LevelWithCage_InvalidLane_ShouldReject()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "cage_invalid_lane";
            levelDef.TotalDistance = 100f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Cage,
                    StartDistance = 20f,
                    Length = 20f,
                    Cages = new[]
                    {
                        new CageSpawnSpec { CageId = "cage_01", LaneIndex = -1 },
                        new CageSpawnSpec { CageId = "cage_02", LaneIndex = 2 }
                    }
                }
            };

            var result = LevelValidator.Validate(levelDef);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.Errors.Count(e => e.Contains("invalid LaneIndex") || e.Contains("LaneIndex")));
        }

        [Test]
        public void Validate_LevelWithCage_EmptyCageIdOrNullSpec_ShouldReject()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "cage_empty_id";
            levelDef.TotalDistance = 100f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Cage,
                    StartDistance = 20f,
                    Length = 20f,
                    Cages = new[]
                    {
                        new CageSpawnSpec { CageId = "", LaneIndex = 0 },
                        null
                    }
                }
            };

            var result = LevelValidator.Validate(levelDef);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("empty CageId")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("is null")));
        }

        [Test]
        public void Validate_LevelWithCage_NegativeOverrides_ShouldReject()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "cage_negative_overrides";
            levelDef.TotalDistance = 100f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Cage,
                    StartDistance = 20f,
                    Length = 20f,
                    Cages = new[]
                    {
                        new CageSpawnSpec { CageId = "cage_01", LaneIndex = 0, CustomHp = -10, CustomSurvivors = -1 }
                    }
                }
            };

            var result = LevelValidator.Validate(levelDef);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("negative CustomHp")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("negative CustomSurvivors")));
        }

        [Test]
        public void Validate_LevelWithCage_ValidSpec_ShouldPass()
        {
            var levelDef = CreateScriptableObject<LevelDefinition>();
            levelDef.LevelId = "cage_valid_spec";
            levelDef.TotalDistance = 100f;
            levelDef.LaneCount = 2;
            levelDef.Segments = new[]
            {
                new SegmentDefinition
                {
                    SegmentType = SegmentType.Cage,
                    StartDistance = 20f,
                    Length = 20f,
                    Cages = new[]
                    {
                        new CageSpawnSpec { CageId = "cage_01", LaneIndex = 0, CustomHp = 60, CustomSurvivors = 3 },
                        new CageSpawnSpec { CageId = "cage_02", LaneIndex = 1 }
                    }
                }
            };

            var result = LevelValidator.Validate(levelDef);
            Assert.IsTrue(result.IsValid);
        }
    }
}
