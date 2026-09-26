#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class Cli
    {
        public static void Compile()
        {
            Debug.Log("[Game.Editor.Cli] Compile succeeded.");
            EditorApplication.Exit(0);
        }

        public static void ImportContent()
        {
            Debug.Log("[Game.Editor.Cli] ImportContent stub.");
            EditorApplication.Exit(0);
        }

        public static void ValidateContent()
        {
            Debug.Log("[Game.Editor.Cli] ValidateContent stub.");
            EditorApplication.Exit(0);
        }

        public static void SimulateLevel()
        {
            Debug.Log("[Game.Editor.Cli] SimulateLevel stub.");
            EditorApplication.Exit(0);
        }

        public static void BuildAndroid()
        {
            Debug.Log("[Game.Editor.Cli] BuildAndroid stub.");
            EditorApplication.Exit(0);
        }
    }
}
#endif
