#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
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
            public string weaponId;
            public string stat;
            public string kind;
            public float value;
        }

        [MenuItem("Horde Runner/Content/Import Perks")]
        public static void ImportAllMenuItem()
        {
            int count = ImportAll();
            Debug.Log($"[Game.Editor.PerkImporter] Imported {count} perks from '{DefaultSourcePath}' to '{DefaultTargetPath}'.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? DefaultSourcePath : sourceFolder;
            string target = string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder;
            var errorSink = new List<string>();

            if (!Directory.Exists(source))
            {
                Debug.LogWarning($"[Game.Editor.PerkImporter] Source directory does not exist: {source}");
                return 0;
            }

            EnsureTargetFolderExists(target);

            string[] files = Directory.GetFiles(source, "*.json");
            int importedCount = 0;
            var catalog = WeaponImporter.LoadCatalog();

            for (int i = 0; i < files.Length; i++)
            {
                var perk = ImportFileInternal(files[i], target, catalog, errorSink);
                if (perk != null)
                {
                    importedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PublishErrors(errorSink, errors);
            return importedCount;
        }

        public static PerkDefinition ImportFile(string jsonFilePath, string targetFolder = null)
        {
            string target = string.IsNullOrEmpty(targetFolder) ? DefaultTargetPath : targetFolder;
            EnsureTargetFolderExists(target);

            var errorSink = new List<string>();
            var perk = ImportFileInternal(jsonFilePath, target, WeaponImporter.LoadCatalog(), errorSink);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            PublishErrors(errorSink, null);
            return perk;
        }

        private static PerkDefinition ImportFileInternal(string jsonFilePath, string targetFolder,
                                                         IWeaponCatalog catalog, List<string> errors)
        {
            if (!File.Exists(jsonFilePath))
            {
                return null;
            }

            string json = File.ReadAllText(jsonFilePath);
            return ImportJsonInternal(json, targetFolder, Path.GetFileNameWithoutExtension(jsonFilePath),
                                      Path.GetFileName(jsonFilePath), catalog, errors);
        }

        private static PerkDefinition ImportJsonInternal(string json, string targetFolder, string fallbackId,
                                                         string fileName, IWeaponCatalog catalog, List<string> errors)
        {
            PerkJsonDto dto;
            try
            {
                dto = JsonUtility.FromJson<PerkJsonDto>(json);
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

            string perkId = !string.IsNullOrEmpty(dto.id) ? dto.id : fallbackId;
            if (string.IsNullOrEmpty(perkId))
            {
                return null;
            }

            int errorCountBefore = errors.Count;
            var effects = new List<IPerkEffect>();
            if (dto.effects != null)
            {
                for (int i = 0; i < dto.effects.Count; i++)
                {
                    var effect = CreateEffect(dto.effects[i], $"{fileName}: effects[{i}]", catalog, errors);
                    if (effect != null)
                    {
                        effects.Add(effect);
                    }
                }
            }

            if (errors.Count > errorCountBefore)
            {
                return null;
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

        private static IPerkEffect CreateEffect(EffectJsonDto dto, string location, IWeaponCatalog catalog,
                                                List<string> errors)
        {
            if (dto == null || string.IsNullOrEmpty(dto.type))
            {
                errors.Add($"{location}.type ausente");
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
                case "weapon":
                    return CreateWeaponEffect(dto, location, catalog, errors);
                case "stat":
                    return CreateStatEffect(dto, location, errors);
                default:
                    errors.Add($"{location}.type '{dto.type}' inválido");
                    return null;
            }
        }

        private static IPerkEffect CreateWeaponEffect(EffectJsonDto dto, string location, IWeaponCatalog catalog,
                                                      List<string> errors)
        {
            if (catalog == null)
            {
                errors.Add($"{location}.weaponId '{dto.weaponId}' sem catálogo de armas (importe as armas antes)");
                return null;
            }

            if (!catalog.TryGet(dto.weaponId, out _))
            {
                errors.Add($"{location}.weaponId '{dto.weaponId}' fora do catálogo");
                return null;
            }

            return new EquipWeaponEffect(dto.weaponId);
        }

        // JsonUtility zera `value` ausente; um modificador 0 seria um portão que não faz nada.
        private static IPerkEffect CreateStatEffect(EffectJsonDto dto, string location, List<string> errors)
        {
            int errorCountBefore = errors.Count;

            if (!JsonEnumNames.TryParse(dto.stat, out StatId stat))
            {
                errors.Add($"{location}.stat '{dto.stat}' inválido");
            }

            if (!JsonEnumNames.TryParse(dto.kind, out ModifierKind kind))
            {
                errors.Add($"{location}.kind '{dto.kind}' inválido");
            }

            if (dto.value == 0f || float.IsNaN(dto.value) || float.IsInfinity(dto.value)
                || (kind == ModifierKind.PercentMultiply && dto.value <= -1f))
            {
                errors.Add($"{location}.value inválido");
            }

            return errors.Count == errorCountBefore ? new ModifyStatEffect(stat, kind, dto.value) : null;
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
                Debug.LogError($"[Game.Editor.PerkImporter] {found[i]}");
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
