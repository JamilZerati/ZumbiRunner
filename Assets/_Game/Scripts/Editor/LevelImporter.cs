using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Data;

namespace Game.Editor
{
    public static class LevelImporter
    {
        [MenuItem("Tools/Game/Import Levels")]
        public static int ImportAll(List<string> errors = null)
        {
            string sourceFolder = "Content/Source/Levels";
            string destFolder = "Assets/_Game/Data/Levels";
            int count = 0;

            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            if (!Directory.Exists(sourceFolder))
            {
                errors?.Add($"Source folder {sourceFolder} does not exist.");
                return count;
            }

            string[] files = Directory.GetFiles(sourceFolder, "*.json");
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
