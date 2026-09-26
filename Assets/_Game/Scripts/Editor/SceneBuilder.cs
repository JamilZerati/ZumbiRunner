#if UNITY_EDITOR
using System.IO;
using Game.Composition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class SceneBuilder
    {
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";

        public static void BuildBootstrapScene()
        {
            var directory = Path.GetDirectoryName(BootstrapScenePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scopeObject = new GameObject("GameLifetimeScope");
            scopeObject.AddComponent<GameLifetimeScope>();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] Bootstrap scene built successfully at {BootstrapScenePath}.");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
#endif
