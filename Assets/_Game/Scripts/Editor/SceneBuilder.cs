#if UNITY_EDITOR
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Gameplay;
using Game.Infrastructure.Input;
using Game.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    public static class SceneBuilder
    {
        public const string BootstrapScenePath = "Assets/_Game/Scenes/Bootstrap.unity";
        public const string M1GreyboxScenePath = "Assets/_Game/Scenes/M1_Greybox.unity";

        [MenuItem("Horde Runner/Scenes/Build Bootstrap Scene")]
        public static void BuildBootstrapScene()
        {
            EnsureDirectoryExists(BootstrapScenePath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var scopeObject = new GameObject("GameLifetimeScope");
            scopeObject.AddComponent<GameLifetimeScope>();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] Bootstrap scene built successfully at {BootstrapScenePath}.");
        }

        public static void BuildBootstrapSceneCli()
        {
            BuildBootstrapScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Horde Runner/Scenes/Build M1 Greybox Scene")]
        public static void BuildM1GreyboxScene()
        {
            EnsureDirectoryExists(M1GreyboxScenePath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackGo.name = "Track_Floor";
            trackGo.transform.position = new Vector3(0f, -0.1f, 100f);
            trackGo.transform.localScale = new Vector3(4.5f, 0.2f, 200f);

            var dividerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerGo.name = "Lane_Divider";
            dividerGo.transform.position = new Vector3(0f, 0.02f, 100f);
            dividerGo.transform.localScale = new Vector3(0.08f, 0.05f, 200f);

            var generalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            generalGo.name = "General";
            generalGo.transform.localScale = Vector3.one;

            var input = generalGo.AddComponent<StandaloneLaneInput>();
            var mover = generalGo.AddComponent<LaneMover>();
            mover.Initialize(new LaneLayout(2, 2.0f), input, null, 0);

            var scroller = generalGo.AddComponent<TrackScroller>();
            scroller.ForwardSpeed = 8.0f;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            var followCam = camGo.AddComponent<FollowCamera>();
            followCam.Target = generalGo.transform;
            followCam.Offset = new Vector3(0f, 7.0f, -9.0f);
            followCam.Snap();

            EditorSceneManager.SaveScene(scene, M1GreyboxScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] M1 Greybox scene built successfully at {M1GreyboxScenePath}.");
        }

        public static void BuildM1GreyboxSceneCli()
        {
            BuildM1GreyboxScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void EnsureDirectoryExists(string scenePath)
        {
            var directory = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
#endif
