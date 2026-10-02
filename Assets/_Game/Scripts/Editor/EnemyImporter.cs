#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class EnemyImporter
    {
        public const string DefaultSourcePath = "Content/Source/Enemies";
        public const string DefaultTargetPath = "Assets/_Game/Data/Enemies";
        public const string CatalogAssetName = "EnemyCatalog";

        [Serializable]
        private class EnemyJsonDto
        {
            public string id;
            public int baseHp;
            public float speed;
            public float contactDps;
            public int coinValue;
            public List<BehaviorJsonDto> behaviors;
        }

        [Serializable]
        private class BehaviorJsonDto
        {
            public string type;
            public float delay;
            public float distance;
            public int damage;
            public float radius;
            public float interval;
            public float warningDuration;
            public int healPerSecond;
            public string archetypeId;
        }

        [MenuItem("Horde Runner/Content/Import Enemies")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.EnemyImporter] Imported {count} enemies, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.EnemyImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<EnemyDefinition>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var enemy = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (enemy != null)
                {
                    imported.Add(enemy);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static EnemyCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<EnemyCatalog>(CatalogPath(target));
        }

        private static EnemyDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
                                                          HashSet<string> seenIds, List<string> errors)
        {
            string fileName = Path.GetFileName(jsonFilePath);

            EnemyJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<EnemyJsonDto>(File.ReadAllText(jsonFilePath));
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

            var behaviors = new List<IEnemyBehavior>();
            if (dto.behaviors != null)
            {
                for (int i = 0; i < dto.behaviors.Count; i++)
                {
                    var behavior = CreateBehavior(dto.behaviors[i], $"{fileName}: behaviors[{i}]", errors);
                    if (behavior != null)
                    {
                        behaviors.Add(behavior);
                    }
                }
            }

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
            bool isNew = definition == null;
            if (isNew)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            }

            definition.SetData(dto.id, dto.baseHp, dto.speed, dto.contactDps, dto.coinValue, behaviors.ToArray());

            if (isNew)
            {
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static IEnemyBehavior CreateBehavior(BehaviorJsonDto dto, string location, List<string> errors)
        {
            if (dto == null || string.IsNullOrEmpty(dto.type))
            {
                errors.Add($"{location}.type ausente");
                return null;
            }

            switch (dto.type.ToLowerInvariant())
            {
                case "movestraight": return new MoveStraightBehavior();
                case "chaselane": return new ChaseLaneBehavior { Delay = dto.delay > 0f ? dto.delay : 1f };
                case "stopat": return new StopAtBehavior { Distance = dto.distance > 0f ? dto.distance : 25f };
                case "frontshield": return new FrontShieldBehavior { IsActive = true };
                case "explodeoncontact": return new ExplodeOnContactBehavior { Damage = dto.damage > 0 ? dto.damage : 30, Radius = dto.radius > 0f ? dto.radius : 2f };
                case "explodeondeath": return new ExplodeOnDeathBehavior { Damage = dto.damage > 0 ? dto.damage : 50, Radius = dto.radius > 0f ? dto.radius : 3f };
                case "rangedspit": return new RangedSpitBehavior { Interval = dto.interval > 0f ? dto.interval : 3f, WarningDuration = dto.warningDuration > 0f ? dto.warningDuration : 1f, Damage = dto.damage > 0 ? dto.damage : 30 };
                case "healaura": return new HealAuraBehavior { HealPerSecond = dto.healPerSecond > 0 ? dto.healPerSecond : 5, Radius = dto.radius > 0f ? dto.radius : 4f };
                case "resurrect": return new ResurrectBehavior { Interval = dto.interval > 0f ? dto.interval : 6f, ArchetypeId = !string.IsNullOrEmpty(dto.archetypeId) ? dto.archetypeId : "walker" };
                default:
                    errors.Add($"{location}.type '{dto.type}' inválido");
                    return null;
            }
        }

        private static bool Validate(EnemyJsonDto dto, string fileName, List<string> errors)
        {
            int errorCountBefore = errors.Count;
            if (!IsValidId(dto.id)) errors.Add($"{fileName}: id inválido");
            if (dto.baseHp <= 0) errors.Add($"{fileName}: baseHp inválido");
            if (!(dto.speed > 0f) || float.IsInfinity(dto.speed) || float.IsNaN(dto.speed)) errors.Add($"{fileName}: speed inválido");
            if (dto.contactDps < 0f || float.IsInfinity(dto.contactDps) || float.IsNaN(dto.contactDps)) errors.Add($"{fileName}: contactDps inválido");
            if (dto.coinValue < 0) errors.Add($"{fileName}: coinValue inválido");
            return errors.Count == errorCountBefore;
        }

        private static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !string.Equals(id, CatalogAssetName, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteCatalog(string targetFolder, List<EnemyDefinition> enemies)
        {
            string catalogPath = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<EnemyCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<EnemyCatalog>();
            }

            catalog.SetEnemies(enemies);

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
                Debug.LogError($"[Game.Editor.EnemyImporter] {found[i]}");
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
