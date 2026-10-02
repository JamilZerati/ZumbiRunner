using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Data;
using Game.Editor;

namespace Game.Tests.EditMode
{
    public class LevelImporterTests
    {
        private const string TestLevelFile = "Content/Source/Levels/test_level_temp.json";
        private const string OutputAssetPath = "Assets/_Game/Data/Levels/test_level_temp.asset";

        [SetUp]
        public void SetUp()
        {
            if (!Directory.Exists("Content/Source/Levels"))
            {
                Directory.CreateDirectory("Content/Source/Levels");
            }
            
            string json = @"{
              ""LevelId"": ""test_01"",
              ""TotalDistance"": 100.0,
              ""Speed"": 5.0,
              ""TargetDurationSeconds"": 20.0,
              ""InitialTroops"": 3,
              ""Segments"": [
                {
                  ""SegmentType"": 1,
                  ""StartDistance"": 10.0,
                  ""Length"": 20.0,
                  ""Events"": [
                    {
                      ""DistanceOffset"": 5.0,
                      ""Type"": ""GateSpawn"",
                      ""Data"": ""fireRate+5""
                    }
                  ]
                }
              ]
            }";
            File.WriteAllText(TestLevelFile, json);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(TestLevelFile))
            {
                File.Delete(TestLevelFile);
            }
            if (File.Exists(TestLevelFile + ".meta"))
            {
                File.Delete(TestLevelFile + ".meta");
            }
            if (AssetDatabase.LoadAssetAtPath<LevelDefinition>(OutputAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(OutputAssetPath);
            }
        }

        [Test]
        public void ImportAll_CreatesAsset_WithValidData()
        {
            var errors = new List<string>();
            LevelImporter.ImportAll(errors);

            Assert.IsEmpty(errors);

            var asset = AssetDatabase.LoadAssetAtPath<LevelDefinition>(OutputAssetPath);
            Assert.IsNotNull(asset, "Asset should have been created.");

            Assert.AreEqual("test_01", asset.LevelId);
            Assert.AreEqual(100.0f, asset.TotalDistance);
            Assert.AreEqual(5.0f, asset.Speed);
            Assert.AreEqual(20.0f, asset.TargetDurationSeconds);
            Assert.AreEqual(3, asset.InitialTroops);
            
            Assert.IsNotNull(asset.Segments);
            Assert.AreEqual(1, asset.Segments.Length);
            
            var seg = asset.Segments[0];
            Assert.AreEqual(SegmentType.Gate, seg.SegmentType);
            Assert.AreEqual(10.0f, seg.StartDistance);
            Assert.AreEqual(20.0f, seg.Length);
            
            Assert.IsNotNull(seg.Events);
            Assert.AreEqual(1, seg.Events.Length);
            
            var evt = seg.Events[0];
            Assert.AreEqual(5.0f, evt.DistanceOffset);
            Assert.AreEqual("GateSpawn", evt.Type);
            Assert.AreEqual("fireRate+5", evt.Data);
        }
    }
}
