#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Gates;
using Game.Core.Modifiers;
using Game.Data.Mutations;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class MutationImporter
    {
        public const string DefaultSourcePath = "Content/Source/Mutations";
        public const string DefaultSourceFileName = "mutations.json";
        public const string DefaultTargetPath = "Assets/_Game/Data/Mutations";
        public const string CatalogAssetName = "MutationCatalog";

        [Serializable]
        public class MutationJsonDto
        {
            public string id;
            public string displayName;
            public string description;
            public float rewardBonus;
            public List<ModifierJsonDto> modifiers;
        }

        [Serializable]
        public class ModifierJsonDto
        {
            public string type;
            public string sourceId;
            public string statTarget;
            public float multiplier = 1f;
            public int squadCount;
            public bool remapMultiplyToAdd;
            public bool remapDivideToSubtract;
        }

        [Serializable]
        private class MutationContainerJsonDto
        {
            public List<MutationJsonDto> mutations;
        }

        [MenuItem("Horde Runner/Content/Import Mutations")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.MutationImporter] Imported {count} mutations, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.MutationImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            var imported = new List<MutationDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string mainFile = Path.Combine(source, DefaultSourceFileName);
            if (File.Exists(mainFile))
            {
                ImportFileInternal(mainFile, target, seenIds, imported, errorSink);
            }

            string[] otherFiles = Directory.GetFiles(source, "*.json");
            Array.Sort(otherFiles, StringComparer.Ordinal);
            for (int i = 0; i < otherFiles.Length; i++)
            {
                string file = otherFiles[i];
                if (string.Equals(Path.GetFileName(file), DefaultSourceFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                ImportFileInternal(file, target, seenIds, imported, errorSink);
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static MutationCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<MutationCatalog>(CatalogPath(target));
        }

        private static void ImportFileInternal(string jsonFilePath, string targetFolder,
                                               HashSet<string> seenIds, List<MutationDefinition> imported,
                                               List<string> errors)
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
                return;
            }

            List<MutationJsonDto> dtoList = ParseJson(json, fileName, errors);
            if (dtoList == null || dtoList.Count == 0) return;

            for (int i = 0; i < dtoList.Count; i++)
            {
                var dto = dtoList[i];
                var def = ImportSingleDto(dto, fileName, targetFolder, seenIds, errors);
                if (def != null)
                {
                    imported.Add(def);
                }
            }
        }

        private static List<MutationJsonDto> ParseJson(string json, string fileName, List<string> errors)
        {
            string trimmed = json.Trim();
            if (trimmed.StartsWith("["))
            {
                trimmed = "{\"mutations\":" + trimmed + "}";
            }

            try
            {
                var container = JsonUtility.FromJson<MutationContainerJsonDto>(trimmed);
                if (container != null && container.mutations != null && container.mutations.Count > 0)
                {
                    return container.mutations;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{fileName}: JSON invalido ({ex.Message})");
                return null;
            }

            try
            {
                var single = JsonUtility.FromJson<MutationJsonDto>(trimmed);
                if (single != null && !string.IsNullOrEmpty(single.id))
                {
                    return new List<MutationJsonDto> { single };
                }
            }
            catch
            {
            }

            errors.Add($"{fileName}: nenhuma definicao de mutacao encontrada no JSON");
            return null;
        }

        private static MutationDefinition ImportSingleDto(MutationJsonDto dto, string fileName, string targetFolder,
                                                          HashSet<string> seenIds, List<string> errors)
        {
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

            if (string.IsNullOrEmpty(dto.displayName))
            {
                errors.Add($"{fileName}: displayName ausente para mutacao '{dto.id}'");
                return null;
            }

            if (dto.rewardBonus < 0f)
            {
                errors.Add($"{fileName}: rewardBonus deve ser nao-negativo para '{dto.id}'");
                return null;
            }

            var modifiers = new List<IStageModifier>();
            if (dto.modifiers != null)
            {
                for (int m = 0; m < dto.modifiers.Count; m++)
                {
                    var modDto = dto.modifiers[m];
                    if (modDto == null) continue;

                    string sourceId = string.IsNullOrEmpty(modDto.sourceId) ? $"mutacao:{dto.id}" : modDto.sourceId;
                    string type = modDto.type ?? string.Empty;

                    if (type.Equals("EnemyStat", StringComparison.OrdinalIgnoreCase) ||
                        type.Equals("EnemyStatStageModifier", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Enum.TryParse<StatTarget>(modDto.statTarget, true, out var target))
                        {
                            errors.Add($"{fileName}: statTarget '{modDto.statTarget}' invalido para '{dto.id}'");
                            continue;
                        }
                        float multiplier = modDto.multiplier <= 0f ? 1f : modDto.multiplier;
                        modifiers.Add(new EnemyStatStageModifier(sourceId, target, multiplier));
                    }
                    else if (type.Equals("SquadStat", StringComparison.OrdinalIgnoreCase) ||
                             type.Equals("SquadStatStageModifier", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Enum.TryParse<StatTarget>(modDto.statTarget, true, out var target))
                        {
                            errors.Add($"{fileName}: statTarget '{modDto.statTarget}' invalido para '{dto.id}'");
                            continue;
                        }
                        float multiplier = modDto.multiplier <= 0f ? 1f : modDto.multiplier;
                        modifiers.Add(new SquadStatStageModifier(sourceId, target, multiplier));
                    }
                    else if (type.Equals("InitialSquadOverride", StringComparison.OrdinalIgnoreCase) ||
                             type.Equals("InitialSquadOverrideStageModifier", StringComparison.OrdinalIgnoreCase))
                    {
                        int squadCount = modDto.squadCount <= 0 ? 1 : modDto.squadCount;
                        modifiers.Add(new InitialSquadOverrideStageModifier(sourceId, squadCount));
                    }
                    else if (type.Equals("GateOperationRemap", StringComparison.OrdinalIgnoreCase) ||
                             type.Equals("GateOperationRemapStageModifier", StringComparison.OrdinalIgnoreCase))
                    {
                        modifiers.Add(new GateOperationRemapStageModifier(sourceId, modDto.remapMultiplyToAdd, modDto.remapDivideToSubtract));
                    }
                    else
                    {
                        errors.Add($"{fileName}: tipo de modificador desconhecido '{type}' em '{dto.id}'");
                    }
                }
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<MutationDefinition>(assetPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MutationDefinition>();
                definition.SetData(dto.id, dto.displayName, dto.description, dto.rewardBonus, modifiers);
                AssetDatabase.CreateAsset(definition, assetPath);
            }
            else
            {
                definition.SetData(dto.id, dto.displayName, dto.description, dto.rewardBonus, modifiers);
                EditorUtility.SetDirty(definition);
            }

            return definition;
        }

        private static void WriteCatalog(string targetFolder, List<MutationDefinition> imported)
        {
            string path = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<MutationCatalog>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MutationCatalog>();
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
                Debug.LogError($"[Game.Editor.MutationImporter] {internalErrors[i]}");
            }
        }
    }
}
#endif
