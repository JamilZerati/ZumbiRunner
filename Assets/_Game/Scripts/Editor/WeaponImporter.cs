#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class WeaponImporter
    {
        public const string DefaultSourcePath = "Content/Source/Weapons";
        public const string DefaultTargetPath = "Assets/_Game/Data/Weapons";
        public const string CatalogAssetName = "WeaponCatalog";

        [Serializable]
        private class WeaponJsonDto
        {
            public string id;
            public string displayName;
            public float fireRate;
            public int damage;
            public float projectileSpeed;
            public float range;
            public int projectilesPerShot;
            public float spreadWidth;
        }

        [MenuItem("Horde Runner/Content/Import Weapons")]
        public static void ImportAllMenuItem()
        {
            int count = ImportAll();
            Debug.Log($"[Game.Editor.WeaponImporter] Imported {count} weapons from '{DefaultSourcePath}' to '{DefaultTargetPath}'.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.WeaponImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureAssetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            Array.Sort(files, StringComparer.Ordinal);

            var imported = new List<WeaponDefinition>();
            // Ids que diferem só na caixa colidem no mesmo <id>.asset em sistema de arquivos case-insensitive.
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < files.Length; i++)
            {
                var weapon = ImportFileInternal(files[i], target, seenIds, errorSink);
                if (weapon != null)
                {
                    imported.Add(weapon);
                }
            }

            WriteCatalog(target, imported);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return imported.Count;
        }

        public static WeaponCatalog LoadCatalog(string targetFolder = null)
        {
            string target = NormalizeFolder(string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder);
            return AssetDatabase.LoadAssetAtPath<WeaponCatalog>(CatalogPath(target));
        }

        private static WeaponDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
                                                           HashSet<string> seenIds, List<string> errors)
        {
            string fileName = Path.GetFileName(jsonFilePath);

            WeaponJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<WeaponJsonDto>(File.ReadAllText(jsonFilePath));
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

            string assetPath = $"{targetFolder}/{dto.id}.asset";
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(assetPath);
            bool isNew = weapon == null;
            if (isNew)
            {
                weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            }

            weapon.SetData(dto.id, dto.displayName, dto.fireRate, dto.damage, dto.projectileSpeed,
                           dto.range, dto.projectilesPerShot, dto.spreadWidth);

            if (isNew)
            {
                AssetDatabase.CreateAsset(weapon, assetPath);
            }

            EditorUtility.SetDirty(weapon);
            return weapon;
        }

        // JsonUtility preenche número ausente com 0 sem avisar; ausente e não positivo caem na mesma rejeição.
        // NaN e infinito atravessariam os pisos de WeaponStats.Resolve.
        private static bool Validate(WeaponJsonDto dto, string fileName, List<string> errors)
        {
            int errorCountBefore = errors.Count;

            if (!IsValidId(dto.id))
            {
                errors.Add($"{fileName}: id inválido");
            }

            RequirePositive(dto.fireRate, "fireRate", fileName, errors);
            RequirePositive(dto.damage, "damage", fileName, errors);
            RequirePositive(dto.projectileSpeed, "projectileSpeed", fileName, errors);
            RequirePositive(dto.range, "range", fileName, errors);
            RequirePositive(dto.projectilesPerShot, "projectilesPerShot", fileName, errors);

            if (!(dto.spreadWidth >= 0f) || float.IsInfinity(dto.spreadWidth))
            {
                errors.Add($"{fileName}: spreadWidth inválido");
            }

            return errors.Count == errorCountBefore;
        }

        private static void RequirePositive(float value, string field, string fileName, List<string> errors)
        {
            if (!(value > 0f) || float.IsInfinity(value))
            {
                errors.Add($"{fileName}: {field} inválido");
            }
        }

        private static bool IsValidId(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                && id.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                && !string.Equals(id, CatalogAssetName, StringComparison.OrdinalIgnoreCase);
        }

        private static void WriteCatalog(string targetFolder, List<WeaponDefinition> weapons)
        {
            string catalogPath = CatalogPath(targetFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<WeaponCatalog>(catalogPath);
            bool isNew = catalog == null;
            if (isNew)
            {
                catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            }

            catalog.SetWeapons(weapons);

            if (isNew)
            {
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            EditorUtility.SetDirty(catalog);
        }

        // Sem lista do chamador ninguém mais veria o erro; com lista, quem agrega (Cli) decide como reportar.
        private static void PublishErrors(List<string> found, List<string> callerErrors)
        {
            if (callerErrors != null)
            {
                callerErrors.AddRange(found);
                return;
            }

            for (int i = 0; i < found.Count; i++)
            {
                Debug.LogError($"[Game.Editor.WeaponImporter] {found[i]}");
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
