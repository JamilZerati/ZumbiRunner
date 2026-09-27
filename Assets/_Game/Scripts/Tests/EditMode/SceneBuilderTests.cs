using System.IO;
using Game.Editor;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class SceneBuilderTests
    {
        [Test]
        public void ScenePaths_AreDefinedInExpectedLocations()
        {
            Assert.AreEqual("Assets/_Game/Scenes/Bootstrap.unity", SceneBuilder.BootstrapScenePath);
            Assert.AreEqual("Assets/_Game/Scenes/M1_Greybox.unity", SceneBuilder.M1GreyboxScenePath);
            Assert.AreEqual("Assets/_Game/Scenes/M2_Greybox.unity", SceneBuilder.M2GreyboxScenePath);
        }

        [Test]
        public void BuildM1GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM1GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M1GreyboxScenePath));
        }

        [Test]
        public void BuildM2GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM2GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M2GreyboxScenePath));
        }
    }
}
