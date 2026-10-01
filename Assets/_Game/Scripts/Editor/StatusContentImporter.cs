#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Status;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class StatusContentImporter
    {
        public const string StatusSourcePath = "Content/Source/Statuses";
        public const string InteractionSourcePath = "Content/Source/Interactions";
        public const string DefaultTargetPath = "Assets/_Game/Data/Statuses";
        public const string CatalogAssetName = "StatusCatalog";
        public const string InteractionTableAssetName = "EffectInteractionTable";

        [Serializable]
        private class StatusJsonDto
        {
            public string kind;
            public float duration;
            public float tickInterval;
            public int damagePerTick;
            public int threshold;
            public float slowPercent;
            public int chainCount;
            public float chainRadius;
            public int chainDamage;
            public int damagePerTickPerStack;
            public int maxStacks;
            public int explosionDamagePerStack;
            public float explosionRadius;
        }

        [Serializable]
        private class InteractionJsonDto
        {
            public string id;
            public string requires;
            public string trigger;
            public int minHitDamage;
            public float damageMultiplier;
        }

        [MenuItem("Horde Runner/Content/Import Statuses and Interactions")]
        public static void ImportAllMenuItem()
        {
            int statuses = ImportStatuses();
            int interactions = ImportInteractions();
            Debug.Log($"[Game.Editor.StatusContentImporter] Imported {statuses} statuses and {interactions} interactions.");
        }

        public static int ImportStatuses(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? StatusSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.StatusContentImporter] Source directory does not exist: {source}");
                return 0;
            }

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var seenKinds = new HashSet<StatusKind>();
            var importedEffects = new List<IStatusEffect>();

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string fileName = Path.GetFileName(filePath);

                StatusJsonDto dto;
                try
                {
                    dto = JsonUtility.FromJson<StatusJsonDto>(File.ReadAllText(filePath));
                }
                catch (ArgumentException exception)
                {
                    errorSink.Add($"{fileName}: JSON inválido ({exception.Message})");
                    continue;
                }

                if (dto == null)
                {
                    errorSink.Add($"{fileName}: JSON inválido");
                    continue;
                }

                if (!JsonEnumNames.TryParse<StatusKind>(dto.kind, out var kind))
                {
                    errorSink.Add($"{fileName}: kind '{dto.kind}' inválido");
                    continue;
                }

                if (!seenKinds.Add(kind))
                {
                    errorSink.Add($"{fileName}: kind '{dto.kind}' duplicado");
                    continue;
                }

                int errBefore = errorSink.Count;

                switch (kind)
                {
                    case StatusKind.Burn:
                        RequirePositive(dto.duration, "duration", fileName, errorSink);
                        RequirePositive(dto.tickInterval, "tickInterval", fileName, errorSink);
                        RequirePositive(dto.damagePerTick, "damagePerTick", fileName, errorSink);
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new BurnStatus
                            {
                                Duration = dto.duration,
                                TickInterval = dto.tickInterval,
                                DamagePerTick = dto.damagePerTick
                            });
                        }
                        break;

                    case StatusKind.Slow:
                        RequirePositive(dto.duration, "duration", fileName, errorSink);
                        if (!(dto.slowPercent > 0f && dto.slowPercent < 1f) || float.IsNaN(dto.slowPercent) || float.IsInfinity(dto.slowPercent))
                        {
                            errorSink.Add($"{fileName}: slowPercent inválido");
                        }
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new SlowStatus
                            {
                                Duration = dto.duration,
                                SlowPercent = dto.slowPercent
                            });
                        }
                        break;

                    case StatusKind.Freeze:
                        RequirePositive(dto.duration, "duration", fileName, errorSink);
                        RequireAtLeastOne(dto.threshold, "threshold", fileName, errorSink);
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new FreezeStatus
                            {
                                Duration = dto.duration,
                                Threshold = dto.threshold
                            });
                        }
                        break;

                    case StatusKind.Frozen:
                        RequirePositive(dto.duration, "duration", fileName, errorSink);
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new FrozenStatus
                            {
                                Duration = dto.duration
                            });
                        }
                        break;

                    case StatusKind.Shock:
                        RequireAtLeastOne(dto.chainCount, "chainCount", fileName, errorSink);
                        RequirePositive(dto.chainRadius, "chainRadius", fileName, errorSink);
                        RequirePositive(dto.chainDamage, "chainDamage", fileName, errorSink);
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new ShockStatus
                            {
                                ChainCount = dto.chainCount,
                                ChainRadius = dto.chainRadius,
                                ChainDamage = dto.chainDamage
                            });
                        }
                        break;

                    case StatusKind.Poison:
                        RequirePositive(dto.duration, "duration", fileName, errorSink);
                        RequirePositive(dto.tickInterval, "tickInterval", fileName, errorSink);
                        RequirePositive(dto.damagePerTickPerStack, "damagePerTickPerStack", fileName, errorSink);
                        RequireAtLeastOne(dto.maxStacks, "maxStacks", fileName, errorSink);
                        RequirePositive(dto.explosionDamagePerStack, "explosionDamagePerStack", fileName, errorSink);
                        RequirePositive(dto.explosionRadius, "explosionRadius", fileName, errorSink);
                        if (errorSink.Count == errBefore)
                        {
                            importedEffects.Add(new PoisonStatus
                            {
                                Duration = dto.duration,
                                TickInterval = dto.tickInterval,
                                DamagePerTickPerStack = dto.damagePerTickPerStack,
                                MaxStacks = dto.maxStacks,
                                ExplosionDamagePerStack = dto.explosionDamagePerStack,
                                ExplosionRadius = dto.explosionRadius
                            });
                        }
                        break;

                    default:
                        errorSink.Add($"{fileName}: kind '{kind}' sem importador implementado");
                        break;
                }
            }

            if (seenKinds.Contains(StatusKind.Freeze) && !seenKinds.Contains(StatusKind.Frozen))
            {
                errorSink.Add("Catálogo contém Freeze sem Frozen");
            }

            if (errorSink.Count > 0)
            {
                PublishErrors(errorSink, errors);
                return 0;
            }

            WriteStatusCatalog(target, importedEffects);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return importedEffects.Count;
        }

        public static int ImportInteractions(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? InteractionSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.StatusContentImporter] Source directory does not exist: {source}");
                return 0;
            }

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var catalog = LoadStatusCatalog(target);
            var importedInteractions = new List<EffectInteraction>();

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i];
                string fileName = Path.GetFileName(filePath);

                InteractionJsonDto dto;
                try
                {
                    dto = JsonUtility.FromJson<InteractionJsonDto>(File.ReadAllText(filePath));
                }
                catch (ArgumentException exception)
                {
                    errorSink.Add($"{fileName}: JSON inválido ({exception.Message})");
                    continue;
                }

                if (dto == null)
                {
                    errorSink.Add($"{fileName}: JSON inválido");
                    continue;
                }

                int errBefore = errorSink.Count;

                if (string.IsNullOrWhiteSpace(dto.id))
                {
                    errorSink.Add($"{fileName}: id inválido");
                }

                bool hasValidKind = JsonEnumNames.TryParse<StatusKind>(dto.requires, out var requiredKind);
                if (!hasValidKind)
                {
                    errorSink.Add($"{fileName}: requires '{dto.requires}' inválido");
                }
                else if (catalog != null && !catalog.TryGet(requiredKind, out _))
                {
                    errorSink.Add($"{fileName}: requires '{dto.requires}' fora do catálogo");
                }

                if (!JsonEnumNames.TryParse<InteractionTrigger>(dto.trigger, out var trigger))
                {
                    errorSink.Add($"{fileName}: trigger '{dto.trigger}' inválido");
                }

                if (dto.minHitDamage < 1)
                {
                    errorSink.Add($"{fileName}: minHitDamage inválido");
                }

                if (!(dto.damageMultiplier > 1f) || float.IsNaN(dto.damageMultiplier) || float.IsInfinity(dto.damageMultiplier))
                {
                    errorSink.Add($"{fileName}: damageMultiplier inválido");
                }

                if (errorSink.Count == errBefore)
                {
                    importedInteractions.Add(new EffectInteraction(
                        dto.id,
                        requiredKind,
                        trigger,
                        dto.minHitDamage,
                        dto.damageMultiplier
                    ));
                }
            }

            if (errorSink.Count > 0)
            {
                PublishErrors(errorSink, errors);
                return 0;
            }

            WriteInteractionTable(target, importedInteractions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return importedInteractions.Count;
        }

        public static StatusCatalog LoadStatusCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<StatusCatalog>($"{target}/{CatalogAssetName}.asset");
        }

        public static EffectInteractionTable LoadInteractionTable(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<EffectInteractionTable>($"{target}/{InteractionTableAssetName}.asset");
        }

        private static void RequirePositive(float value, string field, string fileName, List<string> errors)
        {
            if (!(value > 0f) || float.IsNaN(value) || float.IsInfinity(value))
            {
                errors.Add($"{fileName}: {field} inválido");
            }
        }

        private static void RequirePositive(int value, string field, string fileName, List<string> errors)
        {
            if (value <= 0)
            {
                errors.Add($"{fileName}: {field} inválido");
            }
        }

        private static void RequireAtLeastOne(int value, string field, string fileName, List<string> errors)
        {
            if (value < 1)
            {
                errors.Add($"{fileName}: {field} inválido");
            }
        }

        private static void WriteStatusCatalog(string targetFolder, List<IStatusEffect> effects)
        {
            if (!targetFolder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EnsureAssetFolderExists(targetFolder);

            string catalogPath = $"{targetFolder}/{CatalogAssetName}.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<StatusCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<StatusCatalog>();
            }

            catalog.SetEffects(effects);

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            EditorUtility.SetDirty(catalog);
        }

        private static void WriteInteractionTable(string targetFolder, List<EffectInteraction> interactions)
        {
            if (!targetFolder.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            EnsureAssetFolderExists(targetFolder);

            string tablePath = $"{targetFolder}/{InteractionTableAssetName}.asset";
            var table = AssetDatabase.LoadAssetAtPath<EffectInteractionTable>(tablePath);
            bool isNew = table == null;
            if (isNew)
            {
                table = ScriptableObject.CreateInstance<EffectInteractionTable>();
            }

            table.SetInteractions(interactions);

            if (isNew)
            {
                AssetDatabase.CreateAsset(table, tablePath);
            }

            EditorUtility.SetDirty(table);
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
                Debug.LogError($"[Game.Editor.StatusContentImporter] {found[i]}");
            }
        }

        private static string NormalizeFolder(string folder) => folder?.Replace('\\', '/').TrimEnd('/');

        private static void EnsureAssetFolderExists(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureAssetFolderExists(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
#endif
