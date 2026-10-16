using System.Collections.Generic;
using Game.Meta.Save;
using Game.Meta.Stages;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class StageMapTests
    {
        [Test]
        public void InitialState_FirstStageUnlocked_SubsequentStagesLocked()
        {
            var stageMap = new StageMapService();

            Assert.IsTrue(stageMap.IsStageUnlocked("stage_01"));
            Assert.IsFalse(stageMap.IsStageUnlocked("stage_02"));
            Assert.AreEqual(0, stageMap.GetStars("stage_01"));
            Assert.AreEqual("stage_01", stageMap.GetHighestUnlockedStage());
        }

        [Test]
        public void RecordStageResult_ZeroStars_DoesNotUnlockNextStage()
        {
            var stageMap = new StageMapService();

            stageMap.RecordStageResult("stage_01", 0, isBoss: false);

            Assert.IsTrue(stageMap.IsStageUnlocked("stage_01"));
            Assert.IsFalse(stageMap.IsStageUnlocked("stage_02"));
            Assert.AreEqual(0, stageMap.GetStars("stage_01"));
        }

        [Test]
        public void RecordStageResult_PositiveStars_UnlocksNextStage()
        {
            var stageMap = new StageMapService();

            stageMap.RecordStageResult("stage_01", 2, isBoss: false);

            Assert.IsTrue(stageMap.IsStageUnlocked("stage_02"));
            Assert.AreEqual(2, stageMap.GetStars("stage_01"));
            Assert.AreEqual("stage_02", stageMap.GetHighestUnlockedStage());
        }

        [Test]
        public void RecordStageResult_ClampsStars_AndRetainsMaximum()
        {
            var stageMap = new StageMapService();

            stageMap.RecordStageResult("stage_01", 5, isBoss: false);
            Assert.AreEqual(3, stageMap.GetStars("stage_01"));

            stageMap.RecordStageResult("stage_01", 1, isBoss: false);
            Assert.AreEqual(3, stageMap.GetStars("stage_01"));
        }

        [Test]
        public void RecordStageResult_MarksBossStages()
        {
            var stageMap = new StageMapService();

            stageMap.RecordStageResult("stage_05", 3, isBoss: true);

            CollectionAssert.Contains(stageMap.GetAllBossStages(), "stage_05");
        }

        [Test]
        public void CustomStageOrder_EnforcesConfiguredSequence()
        {
            var customOrder = new List<string> { "forest_1", "forest_2", "cave_1" };
            var stageMap = new StageMapService(stageOrder: customOrder);

            Assert.IsTrue(stageMap.IsStageUnlocked("forest_1"));
            Assert.IsFalse(stageMap.IsStageUnlocked("forest_2"));

            stageMap.RecordStageResult("forest_1", 3, isBoss: false);

            Assert.IsTrue(stageMap.IsStageUnlocked("forest_2"));
            Assert.IsFalse(stageMap.IsStageUnlocked("cave_1"));

            stageMap.RecordStageResult("forest_2", 3, isBoss: true);

            Assert.IsTrue(stageMap.IsStageUnlocked("cave_1"));
            Assert.AreEqual("cave_1", stageMap.GetHighestUnlockedStage());
            CollectionAssert.Contains(stageMap.GetAllBossStages(), "forest_2");
        }

        [Test]
        public void MetaStateIntegration_SyncsUnlockedStagesAndStars()
        {
            var state = new MetaState();
            var stageMap = new StageMapService(state);

            stageMap.RecordStageResult("stage_01", 3, isBoss: false);

            Assert.IsTrue(state.UnlockedStages.Contains("stage_01"));
            Assert.IsTrue(state.UnlockedStages.Contains("stage_02"));
            Assert.AreEqual(3, state.StageStars["stage_01"]);
        }
    }
}
