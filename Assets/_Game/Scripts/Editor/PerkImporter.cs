#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class PerkImporter
    {
        public const string DefaultSourcePath = "Content/Source/Perks";
        public const string DefaultTargetPath = "Assets/_Game/Data/Perks";

        [Serializable]
        private class PerkJsonDto
        {
            public string id;
            public string displayName;
            public List<EffectJsonDto> effects;
        }

        [Serializable]
        private class EffectJsonDto
        {
            public string type;
            public int amount;
            public int factor;
            public int divisor;
        }

        [MenuItem("Horde Runner/Content/Import Perks")]
        public static void ImportAllMenuItem()
        {
            int count = ImportAll();
            Debug.Log($"[Game.Editor.PerkImporter] Imported {count} perks from '{DefaultSourcePath}' to '{DefaultTargetPath}'.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder;

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.PerkImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureTargetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            int importedCount = 0;

            for (int i = 0; i < files.Length; i++)
            {
                var perk = ImportFileInternal(files[i], target);
                if (perk != null)
                {
                    importedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return importedCount;
        }

        public static PerkDefinition ImportFile(string jsonFilePath, string targetFolder = null)
        {
            string target = string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder;
            EnsureTargetFolderExists(target);

            var perk = ImportFileInternal(jsonFilePath, target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return perk;
        }

        private static PerkDefinition ImportFileInternal(string jsonFilePath, string targetFolder)
        {
            if (!File.Exists(jsonFilePath))
            {
                return null;
            }

            string json = File.ReadAllText(jsonFilePath);
            return ImportJsonInternal(json, targetFolder, Path.GetFileNameWithoutExtension(jsonFilePath));
        }

        private static PerkDefinition ImportJsonInternal(string json, string targetFolder, string fallbackId)
        {
            var dto = JsonUtility.FromJson<PerkJsonDto>(json);
            if (dto == null)
            {
                return null;
            }

            string perkId = !string.IsNullOrEmpty(dto.id) ? dto.id : fallbackId;
            if (string.IsNullOrEmpty(perkId))
            {
                return null;
            }

            var effects = new List<IPerkEffect>();
            if (dto.effects != null)
            {
                for (int i = 0; i < dto.effects.Count; i++)
                {
                    var effect = CreateEffect(dto.effects[i]);
                    if (effect != null)
                    {
                        effects.Add(effect);
                    }
                }
            }

            string normalizedTarget = targetFolder.Replace('\\', '/').TrimEnd('/');
            string assetPath = $"{normalizedTarget}/{perkId}.asset";

            var perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>(assetPath);
            if (perk == null)
            {
                perk = ScriptableObject.CreateInstance<PerkDefinition>();
                perk.SetData(perkId, dto.displayName, effects);
                AssetDatabase.CreateAsset(perk, assetPath);
            }
            else
            {
                perk.SetData(perkId, dto.displayName, effects);
            }

            EditorUtility.SetDirty(perk);
            return perk;
        }

        private static IPerkEffect CreateEffect(EffectJsonDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.type))
            {
                return null;
            }

            switch (dto.type.ToLowerInvariant())
            {
                case "add":
                    return new AddSoldiersEffect(dto.amount);
                case "subtract":
                    return new AddSoldiersEffect(dto.amount != 0 ? -Math.Abs(dto.amount) : 0);
                case "multiply":
                    int factor = dto.factor != 0 ? dto.factor : dto.amount;
                    return new MultiplySoldiersEffect(factor);
                case "divide":
                    int divisor = dto.divisor != 0 ? dto.divisor : dto.amount;
                    return new DivideSoldiersEffect(divisor);
                default:
                    Debug.LogWarning($"[Game.Editor.PerkImporter] Unsupported perk effect type: '{dto.type}'");
                    return null;
            }
        }

        private static void EnsureTargetFolderExists(string targetFolder)
        {
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
        }
    }
}
#endif
