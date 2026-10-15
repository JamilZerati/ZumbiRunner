#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Weather;
using Game.Data.Weather;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class WeatherImporter
    {
        public const string DefaultSourcePath = "Content/Source/Weather";
        public const string DefaultTargetPath = "Assets/_Game/Data/Weather";
        public const string CatalogAssetName = "WeatherCatalog";

        [Serializable]
        public class WeatherJsonDto
        {
            public string id;
            public string type;
            public string displayName;
            public float enemySpeedMultiplier;
            public float enemyContactDamageMultiplier;
            public float squadRangeMultiplier;
            public string appliedStatus;
        }

        [MenuItem("Horde Runner/Content/Import Weather")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.WeatherImporter] Imported {count} weather definitions, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.WeatherImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<WeatherDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var weather = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (weather != null)
                {
                    imported.Add(weather);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static WeatherCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<WeatherCatalog>(CatalogPath(target));
        }

        private static WeatherDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
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

            WeatherJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<WeatherJsonDto>(json);
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

            if (!Enum.TryParse<WeatherType>(dto.type, true, out var weatherType))
            {
                errors.Add($"{fileName}: type '{dto.type}' invalido");
                return null;
            }

            if (dto.enemySpeedMultiplier <= 0f)
            {
                errors.Add($"{fileName}: enemySpeedMultiplier deve ser positivo");
                return null;
            }

            if (dto.enemyContactDamageMultiplier <= 0f)
            {
                errors.Add($"{fileName}: enemyContactDamageMultiplier deve ser positivo");
                return null;
            }

            if (dto.squadRangeMultiplier <= 0f)
            {
                errors.Add($"{fileName}: squadRangeMultiplier deve ser positivo");
                return null;
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<WeatherDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<WeatherDefinition>();
                definition.SetData(dto.id, weatherType, dto.displayName, dto.enemySpeedMultiplier,
                                   dto.enemyContactDamageMultiplier, dto.squadRangeMultiplier, dto.appliedStatus);
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                definition.SetData(dto.id, weatherType, dto.displayName, dto.enemySpeedMultiplier,
                                   dto.enemyContactDamageMultiplier, dto.squadRangeMultiplier, dto.appliedStatus);
                EditorUtility.SetDirty(definition);
            }

            return definition;
        }

        private static void WriteCatalog(string targetFolder, List<WeatherDefinition> imported)
        {
            string path = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<WeatherCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WeatherCatalog>();
                catalog.SetEntries(imported);
                AssetDatabase.CreateAsset(catalog, path);
            }
            else
            {
                catalog.SetEntries(imported);
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
                Debug.LogError($"[Game.Editor.WeatherImporter] {internalErrors[i]}");
            }
        }
    }
}
#endif
