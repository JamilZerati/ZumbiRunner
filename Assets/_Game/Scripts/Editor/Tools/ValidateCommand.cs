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

        public static int Run(
            string levelsFolder = "Assets/_Game/Data/Levels",
            string abilitiesFolder = "Assets/_Game/Data/Abilities")
        {
            int errorCount = 0;
            int levelCount = 0;
            int abilityCount = 0;

            if (Directory.Exists(levelsFolder))
            {
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
            }
            else
            {
                Debug.LogWarning($"[ValidateCommand] Levels folder not found: {levelsFolder}");
            }

            errorCount += ValidateAbilities(abilitiesFolder, out abilityCount);

            if (errorCount > 0)
            {
                Debug.LogError($"[ValidateCommand] Validation failed with {errorCount} errors across {levelCount} levels and {abilityCount} abilities.");
                return 1;
            }

            Debug.Log($"[ValidateCommand] Validation passed for {levelCount} levels and {abilityCount} abilities (0 errors).");
            return 0;
        }

        public static int ValidateAbilities(string abilitiesFolder, out int abilityCount)
        {
            int errors = 0;
            abilityCount = 0;

            if (!Directory.Exists(abilitiesFolder))
            {
                Debug.LogWarning($"[ValidateCommand] Abilities folder not found: {abilitiesFolder}");
                return 0;
            }

            string[] assetFiles = Directory.GetFiles(abilitiesFolder, "*.asset");
            for (int i = 0; i < assetFiles.Length; i++)
            {
                string assetPath = assetFiles[i].Replace('\\', '/');
                var ability = AssetDatabase.LoadAssetAtPath<GeneralAbilityDefinition>(assetPath);
                if (ability == null)
                {
                    continue;
                }

                abilityCount++;
                var abilityErrors = ValidateAbility(ability);
                if (abilityErrors.Count > 0)
                {
                    errors += abilityErrors.Count;
                    for (int j = 0; j < abilityErrors.Count; j++)
                    {
                        Debug.LogError($"[ValidateCommand] Ability '{ability.Id}' ({assetPath}) validation error: {abilityErrors[j]}");
                    }
                }
            }

            return errors;
        }

        public static List<string> ValidateAbility(GeneralAbilityDefinition ability)
        {
            var errors = new List<string>();
            if (ability == null)
            {
                errors.Add("Ability definition is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(ability.Id))
            {
                errors.Add("Ability Id is required.");
            }

            if (string.IsNullOrWhiteSpace(ability.DisplayName))
            {
                errors.Add("Ability DisplayName is required.");
            }

            if (ability.ChargeKills <= 0)
            {
                errors.Add($"Ability ChargeKills must be greater than zero (was {ability.ChargeKills}).");
            }

            if (ability.Effect == null)
            {
                errors.Add("Ability Effect is required.");
            }

            return errors;
        }
    }
}
