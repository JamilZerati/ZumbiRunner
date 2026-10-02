using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;

namespace Game.Editor.Tools
{
    public static class ValidateCommand
    {
        [MenuItem("Horde Runner/Content/Validate Levels")]
        public static void ValidateMenuItem()
        {
            Run();
        }

        public static int Run(string levelsFolder = "Assets/_Game/Data/Levels")
        {
            int errorCount = 0;
            int levelCount = 0;

            if (!Directory.Exists(levelsFolder))
            {
                Debug.LogWarning($"[ValidateCommand] Levels folder not found: {levelsFolder}");
                return 0;
            }

            string[] assetFiles = Directory.GetFiles(levelsFolder, "*.asset");
            for (int i = 0; i < assetFiles.Length; i++)
            {
                string assetPath = assetFiles[i].Replace('\\', '/');
                var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                if (level == null)
                {
                    continue;
                }

                levelCount++;
                var result = LevelValidator.Validate(level);
                if (!result.IsValid)
                {
                    errorCount += result.Errors.Count;
                    for (int j = 0; j < result.Errors.Count; j++)
                    {
                        Debug.LogError($"[ValidateCommand] Level '{level.LevelId}' ({assetPath}) validation error: {result.Errors[j]}");
                    }
                }
            }

            if (errorCount > 0)
            {
                Debug.LogError($"[ValidateCommand] Validation failed with {errorCount} errors across {levelCount} levels.");
                return 1;
            }

            Debug.Log($"[ValidateCommand] Validation passed for {levelCount} levels (0 errors).");
            return 0;
        }
    }
}
