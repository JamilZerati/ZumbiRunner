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
        public const string DefaultAndroidScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        public const string DefaultAndroidOutputPath = "Builds/Android/HordeRunner.apk";
        public const string AndroidPackageName = "com.jamilzerati.horderunner";

        public static void Compile()
        {
            Debug.Log("[Game.Editor.Cli] Compile succeeded.");
            EditorApplication.Exit(0);
        }

        public static void ImportContent()
        {
            var errors = new List<string>();
            // Armas antes de status -> sinergias -> perks: a validação de perk de arma lê o catálogo recém-gerado.
            int weaponCount = WeaponImporter.ImportAll(errors: errors);
            int statusCount = StatusContentImporter.ImportStatuses(errors: errors);
            int interactionCount = StatusContentImporter.ImportInteractions(errors: errors);
            int perkCount = PerkImporter.ImportAll(errors: errors);
            int enemyCount = EnemyImporter.ImportAll(errors: errors);
            int levelCount = LevelImporter.ImportAll(errors: errors);
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError($"[Game.Editor.Cli] ImportContent error: {errors[i]}");
            }
            Debug.Log($"[Game.Editor.Cli] ImportContent completed: {weaponCount} weapons, {statusCount} statuses, {interactionCount} interactions, {perkCount} perks, {enemyCount} enemies, {levelCount} levels imported, {errors.Count} errors.");
            EditorApplication.Exit(ComputeImportExitCode(errors));
        }

        public static int ComputeImportExitCode(IReadOnlyCollection<string> errors)
        {
            return errors != null && errors.Count > 0 ? 1 : 0;
        }

        public static void ValidateContent()
        {
            int exitCode = Tools.ValidateCommand.Run();
            EditorApplication.Exit(exitCode);
        }

        public static void SimulateLevel()
        {
            int exitCode = Tools.SimulateCommand.ExecuteFromCommandLine();
            EditorApplication.Exit(exitCode);
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
