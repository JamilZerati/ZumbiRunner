#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor
{
    public static class Cli
    {
        public const string DefaultAndroidScenePath = "Assets/_Game/Scenes/M4_Greybox.unity";
        public const string DefaultAndroidOutputPath = "Builds/Android/HordeRunner.apk";
        public const string AndroidPackageName = "com.jamilzerati.horderunner";

        public static void Compile()
        {
            Debug.Log("[Game.Editor.Cli] Compile succeeded.");
            EditorApplication.Exit(0);
        }

        public static void ImportContent()
        {
            int count = PerkImporter.ImportAll();
            Debug.Log($"[Game.Editor.Cli] ImportContent completed: {count} perks imported.");
            EditorApplication.Exit(0);
        }

        public static int ComputeImportExitCode(IReadOnlyCollection<string> errors)
        {
            throw new System.NotImplementedException();
        }

        public static void ValidateContent()
        {
            Debug.Log("[Game.Editor.Cli] ValidateContent stub.");
            EditorApplication.Exit(0);
        }

        public static void SimulateLevel()
        {
            Debug.Log("[Game.Editor.Cli] SimulateLevel stub.");
            EditorApplication.Exit(0);
        }

        public static BuildPlayerOptions CreateAndroidBuildPlayerOptions(
            string scenePath = DefaultAndroidScenePath,
            string outputPath = DefaultAndroidOutputPath,
            bool isDevelopment = true)
        {
            return new BuildPlayerOptions
            {
                scenes = new[] { scenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = isDevelopment ? BuildOptions.Development : BuildOptions.None
            };
        }

        public static void ConfigureAndroidPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageName);
        }

        public static bool ExecuteAndroidBuild(BuildPlayerOptions options)
        {
            string outputDirectory = Path.GetDirectoryName(options.locationPathName);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            ConfigureAndroidPlayerSettings();

            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool success = report.summary.result == BuildResult.Succeeded;

            if (success)
            {
                Debug.Log($"[Game.Editor.Cli] Android build succeeded: {options.locationPathName} ({report.summary.totalSize} bytes)");
            }
            else
            {
                Debug.LogError($"[Game.Editor.Cli] Android build failed. Result: {report.summary.result}, Errors: {report.summary.totalErrors}");
            }

            return success;
        }

        public static void BuildAndroid()
        {
            BuildPlayerOptions options = CreateAndroidBuildPlayerOptions();
            bool success = ExecuteAndroidBuild(options);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
#endif
