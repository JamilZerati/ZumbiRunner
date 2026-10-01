using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class CliBuildAndroidTests
    {
        [Test]
        public void CreateAndroidBuildPlayerOptions_DefaultParameters_ReturnsExpectedBuildOptions()
        {
            BuildPlayerOptions options = Cli.CreateAndroidBuildPlayerOptions();

            Assert.IsNotNull(options.scenes);
            Assert.AreEqual(1, options.scenes.Length);
            Assert.AreEqual(Cli.DefaultAndroidScenePath, options.scenes[0]);
            Assert.AreEqual(Cli.DefaultAndroidOutputPath, options.locationPathName);
            Assert.AreEqual(BuildTarget.Android, options.target);
            Assert.AreEqual(BuildTargetGroup.Android, options.targetGroup);
            Assert.AreEqual(BuildOptions.Development, options.options);
        }

        [Test]
        public void CreateAndroidBuildPlayerOptions_CustomParameters_ReturnsConfiguredOptions()
        {
            const string customScene = "Assets/_Game/Scenes/CustomScene.unity";
            const string customOutput = "Builds/Android/Custom.apk";

            BuildPlayerOptions options = Cli.CreateAndroidBuildPlayerOptions(
                scenePath: customScene,
                outputPath: customOutput,
                isDevelopment: false
            );

            Assert.AreEqual(1, options.scenes.Length);
            Assert.AreEqual(customScene, options.scenes[0]);
            Assert.AreEqual(customOutput, options.locationPathName);
            Assert.AreEqual(BuildTarget.Android, options.target);
            Assert.AreEqual(BuildTargetGroup.Android, options.targetGroup);
            Assert.AreEqual(BuildOptions.None, options.options);
        }

        [Test]
        public void ConfigureAndroidPlayerSettings_SetsPortraitOrientationAndDisablesLandscape()
        {
            Cli.ConfigureAndroidPlayerSettings();

            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);
            Assert.AreEqual(Cli.AndroidPackageName, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
        }

        [Test]
        public void DefaultAndroidScenePath_FileExistsOnDisk()
        {
            Assert.AreEqual("Assets/_Game/Scenes/M5_Greybox.unity", Cli.DefaultAndroidScenePath);
            Assert.IsTrue(File.Exists(Cli.DefaultAndroidScenePath), $"Scene file not found at {Cli.DefaultAndroidScenePath}");
        }

        [Test]
        public void DefaultAndroidOutputPath_DirectoryCanBeCreated()
        {
            string outputDirectory = Path.GetDirectoryName(Cli.DefaultAndroidOutputPath);
            Assert.IsFalse(string.IsNullOrEmpty(outputDirectory));

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            Assert.IsTrue(Directory.Exists(outputDirectory));
        }
    }
}
