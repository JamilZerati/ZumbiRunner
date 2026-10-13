using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Core.Gates;
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
            string abilitiesFolder = "Assets/_Game/Data/Abilities",
            string gatesFolder = "Assets/_Game/Data/Gates",
            string cagesFolder = "Assets/_Game/Data/Cages")
        {
            int errorCount = 0;
            int levelCount = 0;
            int abilityCount = 0;
            int gateCount = 0;
            int cageCount = 0;

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
            errorCount += ValidateGates(gatesFolder, out gateCount);
            errorCount += ValidateCages(cagesFolder, out cageCount);

            if (errorCount > 0)
            {
                Debug.LogError($"[ValidateCommand] Validation failed with {errorCount} errors across {levelCount} levels, {abilityCount} abilities, {gateCount} gates, and {cageCount} cages.");
                return 1;
            }

            Debug.Log($"[ValidateCommand] Validation passed for {levelCount} levels, {abilityCount} abilities, {gateCount} gates, and {cageCount} cages (0 errors).");
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

        public static int ValidateGates(string gatesFolder, out int gateCount)
        {
            int errors = 0;
            gateCount = 0;
            if (!Directory.Exists(gatesFolder)) return 0;

            foreach (var file in Directory.GetFiles(gatesFolder, "*.asset"))
            {
                if (Path.GetFileNameWithoutExtension(file).Equals("GateCatalog", StringComparison.OrdinalIgnoreCase)) continue;
                var gate = AssetDatabase.LoadAssetAtPath<GateDefinition>(file.Replace('\\', '/'));
                if (gate == null) continue;
                gateCount++;
                var gateErrors = ValidateGate(gate);
                errors += gateErrors.Count;
                foreach (var err in gateErrors) Debug.LogError($"[ValidateCommand] Gate '{gate.Id}' validation error: {err}");
            }
            return errors;
        }

        public static List<string> ValidateGate(GateDefinition gate)
        {
            var errors = new List<string>();
            if (gate == null) { errors.Add("Gate definition is null."); return errors; }
            if (string.IsNullOrWhiteSpace(gate.Id)) errors.Add("Gate Id is required.");
            if (string.IsNullOrWhiteSpace(gate.DisplayName)) errors.Add("Gate DisplayName is required.");
            if (string.IsNullOrWhiteSpace(gate.PerkId)) errors.Add("Gate PerkId is required.");
            if (gate.Perk == null) errors.Add("Gate Perk reference is required.");
            if (gate.Variant == null) errors.Add("Gate Variant is required.");
            else if (gate.Variant is ShootToUpgradeGateVariant up && !up.Validate(out string upErr)) errors.Add($"Gate variant validation error: {upErr}");
            else if (gate.Variant is CursedGateVariant cursed && !cursed.Validate(out string cErr)) errors.Add($"Gate variant validation error: {cErr}");
            else if (gate.Variant is GuardedGateVariant guarded && !guarded.Validate(out string gErr)) errors.Add($"Gate variant validation error: {gErr}");
            return errors;
        }

        public static int ValidateCages(string cagesFolder, out int cageCount, string enemiesFolder = "Assets/_Game/Data/Enemies")
        {
            int errors = 0;
            cageCount = 0;
            if (!Directory.Exists(cagesFolder)) return 0;

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.GetFiles(cagesFolder, "*.asset"))
            {
                if (Path.GetFileNameWithoutExtension(file).Equals("CageCatalog", StringComparison.OrdinalIgnoreCase)) continue;
                var cage = AssetDatabase.LoadAssetAtPath<CageDefinition>(file.Replace('\\', '/'));
                if (cage == null) continue;
                cageCount++;

                if (!string.IsNullOrWhiteSpace(cage.Id))
                {
                    if (seenIds.Contains(cage.Id))
                    {
                        errors++;
                        Debug.LogError($"[ValidateCommand] Duplicate cage Id '{cage.Id}' in file {file}.");
                    }
                    else
                    {
                        seenIds.Add(cage.Id);
                    }
                }

                var cageErrors = ValidateCage(cage, enemiesFolder);
                errors += cageErrors.Count;
                foreach (var err in cageErrors)
                {
                    Debug.LogError($"[ValidateCommand] Cage '{cage.Id}' validation error: {err}");
                }
            }
            return errors;
        }

        public static List<string> ValidateCage(CageDefinition cage, string enemiesFolder = "Assets/_Game/Data/Enemies")
        {
            var errors = new List<string>();
            if (cage == null)
            {
                errors.Add("Cage definition is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(cage.Id))
            {
                errors.Add("Cage Id is required.");
            }

            if (string.IsNullOrWhiteSpace(cage.DisplayName))
            {
                errors.Add("Cage DisplayName is required.");
            }

            if (cage.MaxHp <= 0)
            {
                errors.Add($"Cage '{cage.Id}' MaxHp must be greater than zero (was {cage.MaxHp}).");
            }

            if (cage.SurvivorCount <= 0)
            {
                errors.Add($"Cage '{cage.Id}' SurvivorCount must be greater than zero (was {cage.SurvivorCount}).");
            }

            if (string.IsNullOrWhiteSpace(cage.LostEnemyArchetypeId))
            {
                errors.Add($"Cage '{cage.Id}' LostEnemyArchetypeId is required.");
            }
            else
            {
                string catalogPath = $"{enemiesFolder}/EnemyCatalog.asset";
                var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(catalogPath);
                if (catalog != null && !catalog.TryGet(cage.LostEnemyArchetypeId, out _))
                {
                    errors.Add($"Cage '{cage.Id}' references missing enemy archetype '{cage.LostEnemyArchetypeId}'.");
                }
            }

            return errors;
        }
    }
}
