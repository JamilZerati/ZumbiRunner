#if UNITY_EDITOR
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Gameplay;
using Game.Infrastructure;
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
        public const string M2GreyboxScenePath = "Assets/_Game/Scenes/M2_Greybox.unity";

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

        [MenuItem("Horde Runner/Scenes/Build M2 Greybox Scene")]
        public static void BuildM2GreyboxScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Game.Editor.SceneBuilder] Cannot build scene while in Play Mode. Please exit Play Mode first.");
                return;
            }

            EnsureDirectoryExists(M2GreyboxScenePath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackGo.name = "Track_Floor";
            trackGo.transform.position = new Vector3(0f, -0.1f, 100f);
            trackGo.transform.localScale = new Vector3(4.5f, 0.2f, 200f);
            var trackRenderer = trackGo.GetComponent<Renderer>();
            if (trackRenderer != null)
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (litShader != null)
                {
                    trackRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.18f, 0.20f, 0.22f) };
                }
            }

            var dividerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerGo.name = "Lane_Divider";
            dividerGo.transform.position = new Vector3(0f, 0.02f, 100f);
            dividerGo.transform.localScale = new Vector3(0.08f, 0.05f, 200f);
            var dividerRenderer = dividerGo.GetComponent<Renderer>();
            if (dividerRenderer != null)
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (litShader != null)
                {
                    dividerRenderer.sharedMaterial = new Material(litShader) { color = Color.white };
                }
            }

            var generalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            generalGo.name = "General";
            generalGo.transform.position = new Vector3(-1f, 0.5f, 0f);
            generalGo.transform.localScale = Vector3.one;
            var generalRenderer = generalGo.GetComponent<Renderer>();
            if (generalRenderer != null)
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (litShader != null)
                {
                    generalRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.15f, 0.55f, 0.95f) };
                }
            }

            var input = generalGo.AddComponent<StandaloneLaneInput>();
            var mover = generalGo.AddComponent<LaneMover>();
            mover.Initialize(new LaneLayout(2, 2.0f), input, null, 0);

            var scroller = generalGo.AddComponent<TrackScroller>();
            scroller.ForwardSpeed = 8.0f;

            var squad = generalGo.AddComponent<SquadController>();
            squad.Initialize(3);

            var serializedSquad = new SerializedObject(squad);
            var initialCountProp = serializedSquad.FindProperty("initialCount");
            if (initialCountProp != null)
            {
                initialCountProp.intValue = 3;
                serializedSquad.ApplyModifiedProperties();
            }

            var soldierTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            soldierTemplate.name = "Soldier_Template";
            soldierTemplate.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var soldierView = soldierTemplate.AddComponent<SoldierView>();
            var soldierRenderer = soldierTemplate.GetComponent<Renderer>();
            if (soldierRenderer != null)
            {
                var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (litShader != null)
                {
                    soldierRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.2f, 0.85f, 0.45f) };
                }
            }
            soldierTemplate.SetActive(false);

            var visualGo = new GameObject("SquadVisualController");
            visualGo.transform.SetParent(generalGo.transform, false);
            var squadVisual = visualGo.AddComponent<SquadVisualController>();
            var serializedVisual = new SerializedObject(squadVisual);
            var leaderProp = serializedVisual.FindProperty("leaderTransform");
            if (leaderProp != null)
            {
                leaderProp.objectReferenceValue = generalGo.transform;
            }
            var prefabProp = serializedVisual.FindProperty("soldierPrefab");
            if (prefabProp != null)
            {
                prefabProp.objectReferenceValue = soldierView;
            }
            var squadCtrlVisualProp = serializedVisual.FindProperty("squadController");
            if (squadCtrlVisualProp != null)
            {
                squadCtrlVisualProp.objectReferenceValue = squad;
            }
            serializedVisual.ApplyModifiedProperties();

            var pool = new ObjectPool<SoldierView>(
                factory: () => Object.Instantiate(soldierTemplate, visualGo.transform).GetComponent<SoldierView>(),
                onRent: s => s.gameObject.SetActive(true),
                onReturn: s => s.gameObject.SetActive(false),
                initialCapacity: 5
            );
            squadVisual.Initialize(generalGo.transform, pool, null, 0.5f, 5);
            squadVisual.SynchronizeSquad(squad.SquadCount);

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var hudGo = new GameObject("SquadCountHud", typeof(RectTransform));
            hudGo.transform.SetParent(canvasGo.transform, false);
            var hud = hudGo.AddComponent<SquadCountHud>();

            var textGo = new GameObject("CountText", typeof(RectTransform));
            textGo.transform.SetParent(hudGo.transform, false);
            var rect = textGo.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -80f);
                rect.sizeDelta = new Vector2(400f, 100f);
            }

            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize = 54;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "Tropa: 3";

            var serializedHud = new SerializedObject(hud);
            var countTextProp = serializedHud.FindProperty("countText");
            if (countTextProp != null)
            {
                countTextProp.objectReferenceValue = tmp;
            }
            var hudSquadProp = serializedHud.FindProperty("squadController");
            if (hudSquadProp != null)
            {
                hudSquadProp.objectReferenceValue = squad;
            }
            serializedHud.ApplyModifiedProperties();
            hud.SetCountText(tmp);
            hud.Initialize(null, squad.SquadCount);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            var followCam = camGo.AddComponent<FollowCamera>();
            followCam.Target = generalGo.transform;
            followCam.Offset = new Vector3(0f, 7.0f, -9.0f);
            followCam.Snap();

            EditorSceneManager.SaveScene(scene, M2GreyboxScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] M2 Greybox scene built successfully at {M2GreyboxScenePath}.");
        }

        public static void BuildM2GreyboxSceneCli()
        {
            BuildM2GreyboxScene();
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
