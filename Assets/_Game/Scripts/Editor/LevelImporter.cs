using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;

namespace Game.Editor
{
    public static class LevelImporter
    {
        [MenuItem("Horde Runner/Content/Import Levels")]
        public static void ImportAllMenuItem()
        {
            var errors = new List<string>();
            int count = ImportAll(errors: errors);
            Debug.Log($"[Game.Editor.LevelImporter] Imported {count} levels, {errors.Count} errors.");
        }

        public static int ImportAll(string sourceFolder = null, string targetFolder = null, List<string> errors = null)
        {
            string source = string.IsNullOrEmpty(sourceFolder) ? "Content/Source/Levels" : sourceFolder;
            string destFolder = string.IsNullOrEmpty(targetFolder) ? "Assets/_Game/Data/Levels" : targetFolder;
            int count = 0;

            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            if (!Directory.Exists(source))
            {
                errors?.Add($"Source folder {source} does not exist.");
                return count;
            }

            string[] files = Directory.GetFiles(source, "*.json");
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    
                    // We must create an instance or load existing
                    string assetPath = $"{destFolder}/{Path.GetFileNameWithoutExtension(file)}.asset";
                    LevelDefinition existing = AssetDatabase.LoadAssetAtPath<LevelDefinition>(assetPath);
                    
                    if (existing != null)
                    {
                        JsonUtility.FromJsonOverwrite(json, existing);
                        EditorUtility.SetDirty(existing);
                    }
                    else
                    {
                        LevelDefinition definition = ScriptableObject.CreateInstance<LevelDefinition>();
                        JsonUtility.FromJsonOverwrite(json, definition);
                        AssetDatabase.CreateAsset(definition, assetPath);
                    }
                    count++;
                }
                catch (System.Exception ex)
                {
                    errors?.Add($"Failed to import {file}: {ex.Message}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return count;
        }
    }
}
