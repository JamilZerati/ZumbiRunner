#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class CageImporter
    {
        public const string DefaultSourcePath = "Content/Source/Cages";
        public const string DefaultTargetPath = "Assets/_Game/Data/Cages";
        public const string CatalogAssetName = "CageCatalog";

        [Serializable]
        private class CageJsonDto
        {
            public string id;
            public string displayName;
            public int maxHp;
            public int survivorCount;
            public string lostEnemyArchetypeId;
        }

        [MenuItem("Horde Runner/Content/Import Cages")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.CageImporter] Imported {count} cages, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null, string enemiesFolder = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string enemies = NormalizeFolder(string.IsNullOrEmpty(enemiesFolder) ? EnemyImporter.DefaultTargetPath : enemiesFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.CageImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<CageDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var cage = ImportFileInternal(files[i], target, enemies, seenIds, errorSink);
                if (cage != null)
                {
                    imported.Add(cage);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static CageCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string path = $"{target}/{CatalogAssetName}.asset";
            return AssetDatabase.LoadAssetAtPath<CageCatalog>(path);
        }

        public static CageDefinition ImportFile(string jsonFilePath, string targetFolder = null, string enemiesFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string enemies = NormalizeFolder(string.IsNullOrEmpty(enemiesFolder) ? EnemyImporter.DefaultTargetPath : enemiesFolder);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            EnsureAssetFolderExists(target);
            var def = ImportFileInternal(jsonFilePath, target, enemies, seenIds, errors);
            PublishErrors(errors, null);
            return def;
        }

        private static CageDefinition ImportFileInternal(
            string jsonFilePath,
            string targetFolder,
            string enemiesFolder,
            HashSet<string> seenIds,
            List<string> errors)
        {
            if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath))
            {
                errors.Add($"JSON file not found: {jsonFilePath}");
                return null;
            }

            CageJsonDto dto;
            try
            {
                string json = File.ReadAllText(jsonFilePath);
                dto = JsonUtility.FromJson<CageJsonDto>(json);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to parse JSON '{jsonFilePath}': {ex.Message}");
                return null;
            }

            if (dto == null)
            {
                errors.Add($"Deserialized DTO was null: {jsonFilePath}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(dto.id))
            {
                errors.Add($"Cage definition has empty id in file: {jsonFilePath}");
                return null;
            }

            if (!seenIds.Add(dto.id))
            {
                errors.Add($"Duplicate cage id '{dto.id}' encountered in file: {jsonFilePath}");
                return null;
            }

            if (dto.maxHp <= 0)
            {
                errors.Add($"Cage '{dto.id}' must have maxHp > 0, but was {dto.maxHp}.");
                return null;
            }

            if (dto.survivorCount <= 0)
            {
                errors.Add($"Cage '{dto.id}' must have survivorCount > 0, but was {dto.survivorCount}.");
                return null;
            }

            string lostArchetype = string.IsNullOrWhiteSpace(dto.lostEnemyArchetypeId) ? "walker" : dto.lostEnemyArchetypeId;
            ValidateEnemyArchetype(dto.id, lostArchetype, enemiesFolder, errors);

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<CageDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<CageDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            string displayName = string.IsNullOrEmpty(dto.displayName) ? dto.id : dto.displayName;
            definition.SetData(dto.id, displayName, dto.maxHp, dto.survivorCount, lostArchetype);
            EditorUtility.SetDirty(definition);

            return definition;
        }

        private static void ValidateEnemyArchetype(string cageId, string archetypeId, string enemiesFolder, List<string> errors)
        {
            if (string.IsNullOrEmpty(archetypeId))
            {
                return;
            }

            string catalogPath = $"{enemiesFolder}/{EnemyImporter.CatalogAssetName}.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(catalogPath);
            if (catalog == null)
            {
                errors.Add($"Cannot validate enemy archetype '{archetypeId}' for cage '{cageId}': EnemyCatalog not found at '{catalogPath}'.");
                return;
            }

            if (!catalog.TryGet(archetypeId, out _))
            {
                errors.Add($"Cage '{cageId}' references missing enemy archetype '{archetypeId}'.");
            }
        }

        private static void WriteCatalog(string targetFolder, List<CageDefinition> cages)
        {
            string catalogPath = $"{targetFolder}/{CatalogAssetName}.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<CageCatalog>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CageCatalog>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            catalog.SetCages(cages);
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureAssetFolderExists(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                return;
            }

            folderPath = NormalizeFolder(folderPath);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureAssetFolderExists(parent);
            }

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static string NormalizeFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            return path.Replace('\\', '/').TrimEnd('/');
        }

        private static void PublishErrors(List<string> errorSink, List<string> output)
        {
            if (errorSink == null || errorSink.Count == 0)
            {
                return;
            }

            if (output != null)
            {
                output.AddRange(errorSink);
                return;
            }

            for (int i = 0; i < errorSink.Count; i++)
            {
                Debug.LogError($"[Game.Editor.CageImporter] {errorSink[i]}");
            }
        }
    }
}
#endif
