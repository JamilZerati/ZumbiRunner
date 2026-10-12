#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Gates;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class GateImporter
    {
        public const string DefaultSourcePath = "Content/Source/Gates";
        public const string DefaultTargetPath = "Assets/_Game/Data/Gates";
        public const string CatalogAssetName = "GateCatalog";

        [Serializable]
        private class GateJsonDto
        {
            public string id;
            public string displayName;
            public string perkId;
            public VariantJsonDto variant;
        }

        [Serializable]
        private class VariantJsonDto
        {
            public string type;
            public int initialValue;
            public int step;
            public int hitsPerStep;
            public int cap;
            public string bonusPerkId;
            public StageModifierJsonDto stageModifier;
            public float durationDistance;
            public string enemyWaveId;
            public string guardArchetypeId;
            public int guardCount;
            public float guardDistanceOffset;
        }

        [Serializable]
        private class StageModifierJsonDto
        {
            public string sourceId;
            public float enemySpeedMultiplier;
            public float enemyContactDamageMultiplier;
        }

        [MenuItem("Horde Runner/Content/Import Gates")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.GateImporter] Imported {count} gates, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null, string perksFolder = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string perks = NormalizeFolder(string.IsNullOrEmpty(perksFolder) ? PerkImporter.DefaultTargetPath : perksFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.GateImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<GateDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var gate = ImportFileInternal(files[i], target, perks, seenIds, errorSink);
                if (gate != null)
                {
                    imported.Add(gate);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static GateDefinition ImportFile(string jsonFilePath, string targetFolder = null, string perksFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            string perks = NormalizeFolder(string.IsNullOrEmpty(perksFolder) ? PerkImporter.DefaultTargetPath : perksFolder);
            EnsureAssetFolderExists(target);

            var errorSink = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var gate = ImportFileInternal(jsonFilePath, target, perks, seenIds, errorSink);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PublishErrors(errorSink, null);
            return gate;
        }

        public static GateCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<GateCatalog>(CatalogPath(target));
        }

        private static GateDefinition ImportFileInternal(string jsonFilePath, string targetFolder, string perksFolder,
                                                          HashSet<string> seenIds, List<string> errors)
        {
            if (!File.Exists(jsonFilePath))
            {
                return null;
            }

            string fileName = Path.GetFileName(jsonFilePath);
            GateJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<GateJsonDto>(File.ReadAllText(jsonFilePath));
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

            int errorCountBefore = errors.Count;
            if (!IsValidId(dto.id))
            {
                errors.Add($"{fileName}: id inválido");
            }
            else if (!seenIds.Add(dto.id))
            {
                errors.Add($"{fileName}: id '{dto.id}' duplicado");
            }

            if (string.IsNullOrWhiteSpace(dto.perkId))
            {
                errors.Add($"{fileName}: perkId ausente");
            }

            var perk = LoadPerk(dto.perkId, perksFolder);
            if (perk == null && !string.IsNullOrWhiteSpace(dto.perkId))
            {
                errors.Add($"{fileName}: perkId '{dto.perkId}' inexistente");
            }

            var variant = CreateVariant(dto.variant, fileName, perksFolder, errors);

            if (errors.Count > errorCountBefore || variant == null)
            {
                return null;
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<GateDefinition>(assetPath);
            bool isNew = definition == null;
            if (isNew)
            {
                definition = ScriptableObject.CreateInstance<GateDefinition>();
            }

            definition.SetData(dto.id, dto.displayName, dto.perkId, perk, variant);

            if (isNew)
            {
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static IGateVariant CreateVariant(VariantJsonDto dto, string fileName, string perksFolder, List<string> errors)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.type))
            {
                errors.Add($"{fileName}: variant.type ausente");
                return null;
            }

            switch (dto.type.ToLowerInvariant())
            {
                case "static":
                    return new StaticGateVariant();

                case "shoottoupgrade":
                case "upgrade":
                    if (dto.hitsPerStep <= 0)
                    {
                        errors.Add($"{fileName}: hitsPerStep deve ser > 0");
                    }
                    if (dto.step == 0)
                    {
                        errors.Add($"{fileName}: step não pode ser 0");
                    }
                    return new ShootToUpgradeGateVariant(dto.initialValue, dto.step, dto.hitsPerStep, dto.cap);

                case "cursed":
                    if (dto.durationDistance <= 0f)
                    {
                        errors.Add($"{fileName}: durationDistance deve ser > 0");
                    }
                    if (!string.IsNullOrEmpty(dto.bonusPerkId))
                    {
                        var bonusPerk = LoadPerk(dto.bonusPerkId, perksFolder);
                        if (bonusPerk == null)
                        {
                            errors.Add($"{fileName}: bonusPerkId '{dto.bonusPerkId}' inexistente");
                        }
                    }
                    var modifierSpec = new StageModifierSpec(
                        dto.stageModifier != null ? dto.stageModifier.sourceId : string.Empty,
                        dto.stageModifier != null ? dto.stageModifier.enemySpeedMultiplier : 1f,
                        dto.stageModifier != null ? dto.stageModifier.enemyContactDamageMultiplier : 1f);
                    return new CursedGateVariant(dto.bonusPerkId, modifierSpec, dto.durationDistance);

                case "guarded":
                    if (dto.guardCount <= 0)
                    {
                        errors.Add($"{fileName}: guardCount deve ser > 0");
                    }
                    return new GuardedGateVariant(dto.enemyWaveId, dto.guardArchetypeId, dto.guardCount, dto.guardDistanceOffset);

                default:
                    errors.Add($"{fileName}: variant.type '{dto.type}' inválido");
                    return null;
            }
        }

        private static PerkDefinition LoadPerk(string perkId, string perksFolder)
        {
            if (string.IsNullOrEmpty(perkId))
            {
                return null;
            }

            string folder = NormalizeFolder(string.IsNullOrEmpty(perksFolder) ? PerkImporter.DefaultTargetPath : perksFolder);
            string assetPath = $"{folder}/{perkId}.asset";
            return AssetDatabase.LoadAssetAtPath<PerkDefinition>(assetPath);
        }

        private static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !string.Equals(id, CatalogAssetName, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteCatalog(string targetFolder, List<GateDefinition> gates)
        {
            string catalogPath = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<GateCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<GateCatalog>();
            }

            catalog.SetGates(gates);

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
                Debug.LogError($"[Game.Editor.GateImporter] {found[i]}");
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
