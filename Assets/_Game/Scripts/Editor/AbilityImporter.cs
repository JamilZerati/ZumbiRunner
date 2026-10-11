#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Abilities;
using Game.Core.Abilities.Effects;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class AbilityImporter
    {
        public const string DefaultSourcePath = "Content/Source/Abilities";
        public const string DefaultTargetPath = "Assets/_Game/Data/Abilities";
        public const string CatalogAssetName = "AbilityCatalog";

        [Serializable]
        private class AbilityJsonDto
        {
            public string id;
            public string displayName;
            public string description;
            public int chargeKills;
            public TargetingJsonDto targeting;
            public EffectJsonDto effect;
        }

        [Serializable]
        private class TargetingJsonDto
        {
            public string type;
            public float maxRange;
            public float radius;
        }

        [Serializable]
        private class EffectJsonDto
        {
            public string type;
        }

        [MenuItem("Horde Runner/Content/Import Abilities")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.AbilityImporter] Imported {count} abilities, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.AbilityImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<GeneralAbilityDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var ability = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (ability != null)
                {
                    imported.Add(ability);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static AbilityCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<AbilityCatalog>(CatalogPath(target));
        }

        private static GeneralAbilityDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
                                                                   HashSet<string> seenIds, List<string> errors)
        {
            string fileName = Path.GetFileName(jsonFilePath);

            AbilityJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<AbilityJsonDto>(File.ReadAllText(jsonFilePath));
            }
            catch (ArgumentException exception)
            {
                errors.Add($"{fileName}: JSON inválido ({exception.Message})");
                return null;
            }

            if (dto == null)
            {
                errors.Add($"{fileName}: JSON inválido");
                return null;
            }

            if (!Validate(dto, fileName, errors))
            {
                return null;
            }

            if (!seenIds.Add(dto.id))
            {
                errors.Add($"{fileName}: id '{dto.id}' duplicado");
                return null;
            }

            var effect = CreateEffect(dto.effect, fileName, errors);
            if (effect == null)
            {
                return null;
            }

            var targeting = CreateTargeting(dto.targeting);

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<GeneralAbilityDefinition>(assetPath);
            bool isNew = definition == null;
            if (isNew)
            {
                definition = ScriptableObject.CreateInstance<GeneralAbilityDefinition>();
            }

            definition.SetData(dto.id, dto.displayName, dto.description, dto.chargeKills, effect, targeting);

            if (isNew)
            {
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static IAbilityEffect CreateEffect(EffectJsonDto dto, string fileName, List<string> errors)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.type))
            {
                errors.Add($"{fileName}: effect ausente");
                return null;
            }

            if (string.Equals(dto.type.Trim(), "GrenadeAbilityEffect", StringComparison.OrdinalIgnoreCase))
            {
                return new GrenadeAbilityEffect();
            }

            errors.Add($"{fileName}: effect.type '{dto.type}' desconhecido");
            return null;
        }

        private static AbilityTargetingConfig CreateTargeting(TargetingJsonDto dto)
        {
            var config = new AbilityTargetingConfig();
            if (dto == null) return config;

            if (Enum.TryParse<AbilityTargetingType>(dto.type, true, out var parsedType))
            {
                config.Type = parsedType;
            }
            
            config.MaxRange = dto.maxRange;
            if (dto.radius > 0) config.Radius = dto.radius;
            
            return config;
        }

        private static bool Validate(AbilityJsonDto dto, string fileName, List<string> errors)
        {
            int errorCountBefore = errors.Count;
            if (!IsValidId(dto.id)) errors.Add($"{fileName}: id inválido");
            if (dto.chargeKills <= 0) errors.Add($"{fileName}: chargeKills inválido");
            if (dto.effect == null || string.IsNullOrWhiteSpace(dto.effect.type)) errors.Add($"{fileName}: effect ausente");
            return errors.Count == errorCountBefore;
        }

        private static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !string.Equals(id, CatalogAssetName, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteCatalog(string targetFolder, List<GeneralAbilityDefinition> abilities)
        {
            string catalogPath = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<AbilityCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<AbilityCatalog>();
            }

            catalog.SetAbilities(abilities);

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            EditorUtility.SetDirty(catalog);
        }

        private static void PublishErrors(List<string> found, List<string> callerErrors)
        {
            if (callerErrors != null)
            {
                callerErrors.AddRange(found);
                return;
            }

            for (int i = 0; i < found.Count; i++)
            {
                Debug.LogError($"[Game.Editor.AbilityImporter] {found[i]}");
            }
        }

        private static string CatalogPath(string targetFolder) => $"{targetFolder}/{CatalogAssetName}.asset";

        private static string NormalizeFolder(string folder) => folder.Replace('\\', '/').TrimEnd('/');

        private static void EnsureAssetFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            if (!folder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                Directory.CreateDirectory(folder);
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureAssetFolderExists(parent);
            }

            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }
    }
}
#endif
