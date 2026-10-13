#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class HoldoutImporter
    {
        public const string DefaultSourcePath = "Content/Source/Holdouts";
        public const string DefaultTargetPath = "Assets/_Game/Data/Holdouts";
        public const string CatalogAssetName = "HoldoutCatalog";

        [Serializable]
        private class HoldoutJsonDto
        {
            public string id;
            public string displayName;
            public int maxHp;
            public int totalSurvivors;
            public float durationSeconds;
            public float zombieContactDps;
            public float waveIntervalSeconds;
            public int zombiesPerWavePerLane;
        }

        [MenuItem("Horde Runner/Content/Import Holdouts")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError($"[Game.Editor.HoldoutImporter] {errors[i]}");
            }
            if (errors.Count > 0)
            {
                Debug.LogError($"[Game.Editor.HoldoutImporter] Holdout import completed with {errors.Count} errors across {count} imported holdouts.");
            }
            else
            {
                Debug.Log($"[Game.Editor.HoldoutImporter] Successfully imported {count} holdouts (0 errors).");
            }
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                errorSink.Add($"Source directory does not exist: {source}");
                PublishErrors(errorSink, errors);
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<HoldoutDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var holdout = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (holdout != null)
                {
                    imported.Add(holdout);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static HoldoutCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string path = $"{target}/{CatalogAssetName}.asset";
            return AssetDatabase.LoadAssetAtPath<HoldoutCatalog>(path);
        }

        public static HoldoutDefinition ImportFile(string jsonFilePath, string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            EnsureAssetFolderExists(target);
            var def = ImportFileInternal(jsonFilePath, target, seenIds, errors);
            PublishErrors(errors, null);
            return def;
        }

        private static HoldoutDefinition ImportFileInternal(
            string jsonFilePath,
            string targetFolder,
            HashSet<string> seenIds,
            List<string> errors)
        {
            if (string.IsNullOrEmpty(jsonFilePath) || !File.Exists(jsonFilePath))
            {
                errors.Add($"JSON file not found: {jsonFilePath}");
                return null;
            }

            HoldoutJsonDto dto;
            try
            {
                string json = File.ReadAllText(jsonFilePath);
                dto = JsonUtility.FromJson<HoldoutJsonDto>(json);
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
                errors.Add($"Holdout definition has empty id in file: {jsonFilePath}");
                return null;
            }

            if (!seenIds.Add(dto.id))
            {
                errors.Add($"Duplicate holdout id '{dto.id}' encountered in file: {jsonFilePath}");
                return null;
            }

            if (dto.maxHp <= 0)
            {
                errors.Add($"Holdout '{dto.id}' must have maxHp > 0, but was {dto.maxHp}.");
                return null;
            }

            if (dto.totalSurvivors < 0)
            {
                errors.Add($"Holdout '{dto.id}' must have totalSurvivors >= 0, but was {dto.totalSurvivors}.");
                return null;
            }

            if (dto.durationSeconds <= 0f)
            {
                errors.Add($"Holdout '{dto.id}' must have durationSeconds > 0, but was {dto.durationSeconds}.");
                return null;
            }

            if (dto.zombieContactDps < 0f)
            {
                errors.Add($"Holdout '{dto.id}' must have zombieContactDps >= 0, but was {dto.zombieContactDps}.");
                return null;
            }

            if (dto.waveIntervalSeconds <= 0f)
            {
                errors.Add($"Holdout '{dto.id}' must have waveIntervalSeconds > 0, but was {dto.waveIntervalSeconds}.");
                return null;
            }

            if (dto.zombiesPerWavePerLane <= 0)
            {
                errors.Add($"Holdout '{dto.id}' must have zombiesPerWavePerLane > 0, but was {dto.zombiesPerWavePerLane}.");
                return null;
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = GetOrCreateAsset<HoldoutDefinition>(assetPath);

            string displayName = string.IsNullOrEmpty(dto.displayName) ? dto.id : dto.displayName;
            definition.SetData(
                dto.id,
                displayName,
                dto.maxHp,
                dto.totalSurvivors,
                dto.durationSeconds,
                dto.zombieContactDps,
                dto.waveIntervalSeconds,
                dto.zombiesPerWavePerLane);
            EditorUtility.SetDirty(definition);

            return definition;
        }

        private static T GetOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            return asset;
        }

        private static void WriteCatalog(string targetFolder, List<HoldoutDefinition> holdouts)
        {
            string catalogPath = $"{targetFolder}/{CatalogAssetName}.asset";
            var catalog = GetOrCreateAsset<HoldoutCatalog>(catalogPath);

            catalog.SetHoldouts(holdouts);
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
                Debug.LogError($"[Game.Editor.HoldoutImporter] {errorSink[i]}");
            }
        }
    }
}
#endif
