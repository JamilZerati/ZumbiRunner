using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Data;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BossDataImporterTests
    {
        [Test]
        public void SegmentType_Boss_HasExpectedEnumValueSix()
        {
            Assert.AreEqual(6, (int)SegmentType.Boss);
        }

        [Test]
        public void LevelDefinition_StoresBossStageFlags()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.LevelId = "level_05_boss";
            level.IsBossStage = true;
            level.BossId = "boss_colossus";

            Assert.IsTrue(level.IsBossStage);
            Assert.AreEqual("boss_colossus", level.BossId);
        }

        [Test]
        public void SegmentDefinition_StoresBossSegmentSpec()
        {
            var segment = new SegmentDefinition
            {
                SegmentType = SegmentType.Boss,
                StartDistance = 200f,
                Length = 500f,
                Boss = new BossSegmentSpec
                {
                    BossId = "boss_colossus",
                    CustomMaxHp = 1000,
                    InitialLane = 0,
                    ArenaLength = 500f
                }
            };

            Assert.AreEqual(SegmentType.Boss, segment.SegmentType);
            Assert.IsNotNull(segment.Boss);
            Assert.AreEqual("boss_colossus", segment.Boss.BossId);
            Assert.AreEqual(1000, segment.Boss.CustomMaxHp);
            Assert.AreEqual(0, segment.Boss.InitialLane);
            Assert.AreEqual(500f, segment.Boss.ArenaLength);
        }

        [Test]
        public void BossColossus_CatalogContainsExpectedArchetypeAndBehaviors()
        {
            var errors = new List<string>();
            EnemyImporter.ImportAll(errors: errors);
            Assert.IsEmpty(errors);

            var catalog = EnemyImporter.LoadCatalog();
            Assert.IsNotNull(catalog);
            Assert.IsTrue(catalog.TryGet("boss_colossus", out var bossDef));
            Assert.AreEqual("boss_colossus", bossDef.Id);
            Assert.AreEqual(1000, bossDef.BaseHp);
            Assert.AreEqual(1.0f, bossDef.Speed);
            Assert.AreEqual(20.0f, bossDef.ContactDps);
            Assert.AreEqual(100, bossDef.CoinValue);

            Assert.IsNotNull(bossDef.Behaviors);
            Assert.AreEqual(3, bossDef.Behaviors.Length);

            var pingPong = bossDef.Behaviors.OfType<BossPingPongBehavior>().FirstOrDefault();
            Assert.IsNotNull(pingPong);
            Assert.AreEqual(0f, pingPong.MinLane);
            Assert.AreEqual(2f, pingPong.MaxLane);
            Assert.AreEqual(2f, pingPong.Speed);

            var dive = bossDef.Behaviors.OfType<BossLaneTelegraphDiveBehavior>().FirstOrDefault();
            Assert.IsNotNull(dive);
            Assert.AreEqual(4f, dive.Interval);
            Assert.AreEqual(1f, dive.TelegraphDuration);
            Assert.AreEqual(50, dive.Damage);

            var summon = bossDef.Behaviors.OfType<BossSummonWaveBehavior>().FirstOrDefault();
            Assert.IsNotNull(summon);
            Assert.AreEqual(3, summon.Thresholds.Length);
            Assert.AreEqual(3, summon.MinionArchetypes.Length);
        }

        [Test]
        public void Level05Boss_ImportsWithBossStageAndSegmentSpec()
        {
            var errors = new List<string>();
            LevelImporter.ImportAll(errors: errors);
            Assert.IsEmpty(errors);

            var levelAsset = AssetDatabase.LoadAssetAtPath<LevelDefinition>("Assets/_Game/Data/Levels/level_05_boss.asset");
            Assert.IsNotNull(levelAsset);
            Assert.AreEqual("level_05_boss", levelAsset.LevelId);
            Assert.IsTrue(levelAsset.IsBossStage);
            Assert.AreEqual("boss_colossus", levelAsset.BossId);

            var bossSegment = levelAsset.Segments.FirstOrDefault(s => s.SegmentType == SegmentType.Boss);
            Assert.IsNotNull(bossSegment);
            Assert.AreEqual(200f, bossSegment.StartDistance);
            Assert.AreEqual(500f, bossSegment.Length);
            Assert.IsNotNull(bossSegment.Boss);
            Assert.AreEqual("boss_colossus", bossSegment.Boss.BossId);
            Assert.AreEqual(1000, bossSegment.Boss.CustomMaxHp);
            Assert.AreEqual(0, bossSegment.Boss.InitialLane);
            Assert.AreEqual(500f, bossSegment.Boss.ArenaLength);
        }
    }
}
