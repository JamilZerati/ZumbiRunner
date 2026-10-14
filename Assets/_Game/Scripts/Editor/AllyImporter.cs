#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Allies;
using Game.Data.Allies;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class AllyImporter
    {
        public const string DefaultSourcePath = "Content/Source/Allies";
        public const string DefaultTargetPath = "Assets/_Game/Data/Allies";
        public const string CatalogAssetName = "AllyCatalog";

        [Serializable]
        private class AllyJsonDto
        {
            public string id;
            public string displayName;
            public string archetype;
            public int maxHealth;
            public float attackInterval;
            public int attackDamage;
            public string statusApplied;
            public string abilityName;
            public float abilityCooldown;
            public int abilityDamage;
            public float petStatModifierValue;
        }

        [MenuItem("Horde Runner/Content/Import Allies")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.AllyImporter] Imported {count} allies, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.AllyImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<AllyDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var ally = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (ally != null)
                {
                    imported.Add(ally);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static AllyCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<AllyCatalog>(CatalogPath(target));
        }

        private static AllyDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
                                                         HashSet<string> seenIds, List<string> errors)
        {
            string fileName = Path.GetFileName(jsonFilePath);
            string json;
            try
            {
                json = File.ReadAllText(jsonFilePath);
            }
            catch (Exception ex)
            {
                errors.Add($"{fileName}: erro de leitura ({ex.Message})");
                return null;
            }

            AllyJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<AllyJsonDto>(json);
            }
            catch (Exception ex)
            {
                errors.Add($"{fileName}: JSON invalido ({ex.Message})");
                return null;
            }

            if (dto == null || string.IsNullOrEmpty(dto.id))
            {
                errors.Add($"{fileName}: id ausente");
                return null;
            }

            if (!seenIds.Add(dto.id))
            {
                errors.Add($"{fileName}: id '{dto.id}' duplicado");
                return null;
            }

            if (!Enum.TryParse<AllyArchetype>(dto.archetype, true, out var archetype))
            {
                errors.Add($"{fileName}: archetype '{dto.archetype}' invalido");
                return null;
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<AllyDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AllyDefinition>();
                definition.SetData(dto.id, dto.displayName, archetype, dto.maxHealth,
                                   dto.attackInterval, dto.attackDamage, dto.statusApplied,
                                   dto.abilityName, dto.abilityCooldown, dto.abilityDamage,
                                   dto.petStatModifierValue);
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                definition.SetData(dto.id, dto.displayName, archetype, dto.maxHealth,
                                   dto.attackInterval, dto.attackDamage, dto.statusApplied,
                                   dto.abilityName, dto.abilityCooldown, dto.abilityDamage,
                                   dto.petStatModifierValue);
                EditorUtility.SetDirty(definition);
            }

            return definition;
        }

        private static void WriteCatalog(string targetFolder, List<AllyDefinition> imported)
        {
            string path = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<AllyCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<AllyCatalog>();
                catalog.SetAllies(imported);
                AssetDatabase.CreateAsset(catalog, path);
            }
            else
            {
                catalog.SetAllies(imported);
                EditorUtility.SetDirty(catalog);
            }
        }

        private static string CatalogPath(string targetFolder) => $"{targetFolder}/{CatalogAssetName}.asset";

        private static string NormalizeFolder(string folder) => folder.Replace('\\', '/').TrimEnd('/');

        private static void EnsureAssetFolderExists(string targetFolder)
        {
            if (AssetDatabase.IsValidFolder(targetFolder)) return;

            string[] parts = targetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void PublishErrors(List<string> internalErrors, List<string> outErrors)
        {
            if (outErrors != null)
            {
                outErrors.AddRange(internalErrors);
                return;
            }
            for (int i = 0; i < internalErrors.Count; i++)
            {
                Debug.LogError($"[Game.Editor.AllyImporter] {internalErrors[i]}");
            }
        }
    }
}
#endif
