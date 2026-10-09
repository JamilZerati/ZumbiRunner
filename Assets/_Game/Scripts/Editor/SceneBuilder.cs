#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.Events;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Abilities;
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
        public const string M3GreyboxScenePath = "Assets/_Game/Scenes/M3_Greybox.unity";
        public const string M4GreyboxScenePath = "Assets/_Game/Scenes/M4_Greybox.unity";
        public const string M5GreyboxScenePath = "Assets/_Game/Scenes/M5_Greybox.unity";
        public const string M6GreyboxScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";

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

        [MenuItem("Horde Runner/Scenes/Build M3 Greybox Scene")]
        public static void BuildM3GreyboxScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Game.Editor.SceneBuilder] Cannot build scene while in Play Mode. Please exit Play Mode first.");
                return;
            }

            EnsureDirectoryExists(M3GreyboxScenePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackGo.name = "Track_Floor";
            trackGo.transform.position = new Vector3(0f, -0.1f, 100f);
            trackGo.transform.localScale = new Vector3(4.5f, 0.2f, 200f);
            var trackRenderer = trackGo.GetComponent<Renderer>();
            if (trackRenderer != null && litShader != null)
            {
                trackRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.18f, 0.20f, 0.22f) };
            }

            var dividerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerGo.name = "Lane_Divider";
            dividerGo.transform.position = new Vector3(0f, 0.02f, 100f);
            dividerGo.transform.localScale = new Vector3(0.08f, 0.05f, 200f);
            var dividerRenderer = dividerGo.GetComponent<Renderer>();
            if (dividerRenderer != null && litShader != null)
            {
                dividerRenderer.sharedMaterial = new Material(litShader) { color = Color.white };
            }

            var generalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            generalGo.name = "General";
            generalGo.transform.position = new Vector3(-1f, 0.5f, 0f);
            generalGo.transform.localScale = Vector3.one;
            var generalRenderer = generalGo.GetComponent<Renderer>();
            if (generalRenderer != null && litShader != null)
            {
                generalRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.15f, 0.55f, 0.95f) };
            }

            var rb = generalGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

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
            if (soldierRenderer != null && litShader != null)
            {
                soldierRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.2f, 0.85f, 0.45f) };
            }
            soldierTemplate.SetActive(false);

            var visualGo = new GameObject("SquadVisualController");
            visualGo.transform.SetParent(generalGo.transform, false);
            var squadVisual = visualGo.AddComponent<SquadVisualController>();
            var serializedVisual = new SerializedObject(squadVisual);
            serializedVisual.FindProperty("leaderTransform").objectReferenceValue = generalGo.transform;
            serializedVisual.FindProperty("soldierPrefab").objectReferenceValue = soldierView;
            serializedVisual.FindProperty("squadController").objectReferenceValue = squad;
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
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
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
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
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
            serializedHud.FindProperty("countText").objectReferenceValue = tmp;
            serializedHud.FindProperty("squadController").objectReferenceValue = squad;
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

            var gatesRoot = new GameObject("Gates");
            CreateGatePair(gatesRoot.transform, 25f, 1, LoadPerk("add_5"), LoadPerk("add_10"), litShader);
            CreateGatePair(gatesRoot.transform, 60f, 2, LoadPerk("multiply_2"), LoadPerk("subtract_3"), litShader);
            CreateGatePair(gatesRoot.transform, 95f, 3, LoadPerk("divide_2"), LoadPerk("multiply_2"), litShader);

            EditorSceneManager.SaveScene(scene, M3GreyboxScenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] M3 Greybox scene built successfully at {M3GreyboxScenePath}.");
        }

        public static void BuildM3GreyboxSceneCli()
        {
            BuildM3GreyboxScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Horde Runner/Scenes/Build M4 Greybox Scene")]
        public static void BuildM4GreyboxScene()
        {
            BuildCombatGreyboxScene(
                M4GreyboxScenePath,
                "M4",
                new[]
                {
                    new GatePairSpec(25f, "add_5", "add_10"),
                    new GatePairSpec(60f, "multiply_2", "subtract_3"),
                    new GatePairSpec(95f, "divide_2", "multiply_2"),
                },
                initialWeaponId: string.Empty,
                loadCatalog: null);
        }

        private readonly struct GatePairSpec
        {
            public readonly float Z;
            public readonly string Lane0PerkId;
            public readonly string Lane1PerkId;

            public GatePairSpec(float z, string lane0PerkId, string lane1PerkId)
            {
                Z = z;
                Lane0PerkId = lane0PerkId;
                Lane1PerkId = lane1PerkId;
            }
        }

        private static void BuildCombatGreyboxScene(
            string scenePath,
            string sceneLabel,
            IReadOnlyList<GatePairSpec> gatePairs,
            string initialWeaponId,
            System.Func<WeaponCatalog> loadCatalog,
            int enemyHealth = 20,
            System.Action<GameObject, EnemyController> setupEnemyStatus = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Game.Editor.SceneBuilder] Cannot build scene while in Play Mode. Please exit Play Mode first.");
                return;
            }

            EnsureDirectoryExists(scenePath);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            lightGo.AddComponent<Light>().type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackGo.name = "Track_Floor";
            trackGo.transform.position = new Vector3(0f, -0.1f, 100f);
            trackGo.transform.localScale = new Vector3(4.5f, 0.2f, 200f);
            var trackRenderer = trackGo.GetComponent<Renderer>();
            if (trackRenderer != null && litShader != null)
            {
                trackRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.18f, 0.20f, 0.22f) };
            }

            var dividerGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dividerGo.name = "Lane_Divider";
            dividerGo.transform.position = new Vector3(0f, 0.02f, 100f);
            dividerGo.transform.localScale = new Vector3(0.08f, 0.05f, 200f);
            var dividerRenderer = dividerGo.GetComponent<Renderer>();
            if (dividerRenderer != null && litShader != null)
            {
                dividerRenderer.sharedMaterial = new Material(litShader) { color = Color.white };
            }

            var generalGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            generalGo.name = "General";
            generalGo.layer = CollisionLayers.SquadBodyLayer;
            generalGo.transform.position = new Vector3(-1f, 0.5f, 0f);
            generalGo.transform.localScale = Vector3.one;
            var generalRenderer = generalGo.GetComponent<Renderer>();
            if (generalRenderer != null && litShader != null)
            {
                generalRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.15f, 0.55f, 0.95f) };
            }

            var rb = generalGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var input = generalGo.AddComponent<StandaloneLaneInput>();
            var mover = generalGo.AddComponent<LaneMover>();
            mover.Initialize(new LaneLayout(2, 2.0f), input, null, 0);

            var scroller = generalGo.AddComponent<TrackScroller>();
            scroller.ForwardSpeed = 8.0f;

            var squad = generalGo.AddComponent<SquadController>();
            squad.Initialize(10);

            var serializedSquad = new SerializedObject(squad);
            var initialCountProp = serializedSquad.FindProperty("initialCount");
            if (initialCountProp != null)
            {
                initialCountProp.intValue = 10;
                serializedSquad.ApplyModifiedProperties();
            }

            var soldierTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            soldierTemplate.name = "Soldier_Template";
            soldierTemplate.layer = CollisionLayers.SquadBodyLayer;
            soldierTemplate.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var soldierView = soldierTemplate.AddComponent<SoldierView>();
            soldierView.EnsurePhysicsSetup();
            var soldierRenderer = soldierTemplate.GetComponent<Renderer>();
            if (soldierRenderer != null && litShader != null)
            {
                soldierRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.2f, 0.85f, 0.45f) };
            }
            soldierTemplate.SetActive(false);

            var visualGo = new GameObject("SquadVisualController");
            visualGo.layer = CollisionLayers.SquadBodyLayer;
            visualGo.transform.SetParent(generalGo.transform, false);
            var squadVisual = visualGo.AddComponent<SquadVisualController>();
            var serializedVisual = new SerializedObject(squadVisual);
            serializedVisual.FindProperty("leaderTransform").objectReferenceValue = generalGo.transform;
            serializedVisual.FindProperty("soldierPrefab").objectReferenceValue = soldierView;
            serializedVisual.FindProperty("squadController").objectReferenceValue = squad;
            serializedVisual.ApplyModifiedProperties();

            var pool = new ObjectPool<SoldierView>(
                factory: () => Object.Instantiate(soldierTemplate, visualGo.transform).GetComponent<SoldierView>(),
                onRent: s => s.gameObject.SetActive(true),
                onReturn: s => s.gameObject.SetActive(false),
                initialCapacity: 10
            );
            squadVisual.Initialize(generalGo.transform, pool, null, 0.5f, 5);
            squadVisual.SynchronizeSquad(squad.SquadCount);

            // Weapon and Projectile Pool
            var weapon = generalGo.AddComponent<WeaponController>();
            weapon.FireRate = 2.0f;
            weapon.DamagePerShot = 2;
            weapon.ProjectileSpeed = 15.0f;
            weapon.MaxDistance = 40.0f;

            // Fora do General pelo mesmo motivo do WeaponController.EnsurePoolInitialized: filho do Rigidbody dele, o acerto vira contato.
            var projectilePoolGo = new GameObject("ProjectilePool");
            projectilePoolGo.layer = CollisionLayers.PlayerProjectileLayer;

            var projectileTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileTemplate.name = "Projectile_Template";
            projectileTemplate.layer = CollisionLayers.PlayerProjectileLayer;
            projectileTemplate.transform.SetParent(projectilePoolGo.transform, false);
            projectileTemplate.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var projCollider = projectileTemplate.GetComponent<SphereCollider>();
            if (projCollider != null)
            {
                projCollider.isTrigger = true;
            }
            var projRb = projectileTemplate.GetComponent<Rigidbody>();
            if (projRb == null)
            {
                projRb = projectileTemplate.AddComponent<Rigidbody>();
            }
            projRb.isKinematic = true;
            projRb.useGravity = false;
            var projView = projectileTemplate.AddComponent<ProjectileView>();
            var projectileComp = projectileTemplate.AddComponent<Projectile>();
            projectileComp.EnsurePhysicsSetup();
            projectileTemplate.SetActive(false);

            var serializedWeapon = new SerializedObject(weapon);
            var projPrefabProp = serializedWeapon.FindProperty("projectilePrefab");
            if (projPrefabProp != null)
            {
                projPrefabProp.objectReferenceValue = projectileComp;
                serializedWeapon.ApplyModifiedProperties();
            }
            // Carregado só depois do NewScene: abrir cena em modo Single descarrega assets sem referência e o campo seria salvo nulo.
            serializedWeapon.FindProperty("catalog").objectReferenceValue = loadCatalog?.Invoke();
            serializedWeapon.FindProperty("initialWeaponId").stringValue = initialWeaponId ?? string.Empty;
            serializedWeapon.ApplyModifiedProperties();

            var projectilePool = new ObjectPool<Projectile>(
                factory: () => Object.Instantiate(projectileTemplate, projectilePoolGo.transform).GetComponent<Projectile>(),
                onRent: p => p.gameObject.SetActive(true),
                onReturn: p => p.gameObject.SetActive(false),
                initialCapacity: 10
            );
            weapon.Initialize(projectilePool, squad);

            // Enemies and HordeSpawner
            var spawnerGo = new GameObject("HordeSpawner");
            spawnerGo.layer = CollisionLayers.EnemyLayer;
            var spawner = spawnerGo.AddComponent<HordeSpawner>();

            var enemiesParent = new GameObject("Enemies");
            enemiesParent.layer = CollisionLayers.EnemyLayer;

            var enemyTemplate = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyTemplate.name = "Enemy_Template";
            enemyTemplate.layer = CollisionLayers.EnemyLayer;
            enemyTemplate.transform.SetParent(enemiesParent.transform, false);
            enemyTemplate.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            var enemyCollider = enemyTemplate.GetComponent<CapsuleCollider>();
            if (enemyCollider != null)
            {
                enemyCollider.isTrigger = true;
            }
            var enemyRb = enemyTemplate.GetComponent<Rigidbody>();
            if (enemyRb == null)
            {
                enemyRb = enemyTemplate.AddComponent<Rigidbody>();
            }
            enemyRb.isKinematic = true;
            enemyRb.useGravity = false;
            var enemyView = enemyTemplate.AddComponent<EnemyView>();
            enemyView.SetupVisuals(new Color(0.85f, 0.2f, 0.2f));
            var enemyHealthComp = enemyTemplate.AddComponent<HealthComponent>();
            enemyHealthComp.Initialize(enemyHealth);
            var serializedEnemyHealth = new SerializedObject(enemyHealthComp);
            var maxHpProp = serializedEnemyHealth.FindProperty("maxHealth");
            if (maxHpProp != null)
            {
                maxHpProp.intValue = enemyHealth;
                serializedEnemyHealth.ApplyModifiedProperties();
            }

            var enemyCtrl = enemyTemplate.AddComponent<EnemyController>();
            enemyCtrl.EnsurePhysicsSetup();
            setupEnemyStatus?.Invoke(enemyTemplate, enemyCtrl);
            enemyTemplate.SetActive(false);

            var serializedSpawner = new SerializedObject(spawner);
            var spawnerPrefabProp = serializedSpawner.FindProperty("enemyPrefab");
            if (spawnerPrefabProp != null)
            {
                spawnerPrefabProp.objectReferenceValue = enemyCtrl;
            }
            var spawnerLaneCountProp = serializedSpawner.FindProperty("laneCount");
            if (spawnerLaneCountProp != null)
            {
                spawnerLaneCountProp.intValue = 2;
            }
            var spawnerLaneWidthProp = serializedSpawner.FindProperty("laneWidth");
            if (spawnerLaneWidthProp != null)
            {
                spawnerLaneWidthProp.floatValue = 2.0f;
            }
            serializedSpawner.ApplyModifiedProperties();

            var enemyPool = new ObjectPool<EnemyController>(
                factory: () => Object.Instantiate(enemyTemplate, enemiesParent.transform).GetComponent<EnemyController>(),
                onRent: e => e.gameObject.SetActive(true),
                onReturn: e => e.gameObject.SetActive(false),
                initialCapacity: 15
            );

            spawner.DefaultEnemyHealth = enemyHealth;
            var laneLayout = new LaneLayout(2, 2.0f);
            spawner.Initialize(laneLayout, enemyPool);

            // Pre-spawn waves: z=35, z=75, z=105
            spawner.SpawnWave(0, 2, 35f, 2f);
            spawner.SpawnWave(1, 2, 35f, 2f);
            spawner.SpawnWave(0, 3, 75f, 2f);
            spawner.SpawnWave(1, 3, 75f, 2f);
            spawner.SpawnWave(0, 4, 105f, 2f);
            spawner.SpawnWave(1, 4, 105f, 2f);

            // CombatDirector
            var eventBus = new EventBus();
            var director = generalGo.AddComponent<CombatDirector>();
            var stateMachine = new GameStateMachine(null, GameState.Boot);
            stateMachine.TryTransition(GameState.Run);
            director.Initialize(squad, scroller, stateMachine, 120f, spawner, eventBus);

            var serializedDirector = new SerializedObject(director);
            var dirSquadProp = serializedDirector.FindProperty("squad");
            if (dirSquadProp != null) dirSquadProp.objectReferenceValue = squad;
            var dirScrollerProp = serializedDirector.FindProperty("scroller");
            if (dirScrollerProp != null) dirScrollerProp.objectReferenceValue = scroller;
            var dirSpawnerProp = serializedDirector.FindProperty("spawner");
            if (dirSpawnerProp != null) dirSpawnerProp.objectReferenceValue = spawner;
            var dirVictoryProp = serializedDirector.FindProperty("victoryDistance");
            if (dirVictoryProp != null) dirVictoryProp.floatValue = 120f;
            serializedDirector.ApplyModifiedProperties();

            // GeneralAbilityController
            var abilityDef = AssetDatabase.LoadAssetAtPath<GeneralAbilityDefinition>("Assets/_Game/Data/Abilities/grenade.asset");
            var abilityController = generalGo.AddComponent<GeneralAbilityController>();
            var serializedAbility = new SerializedObject(abilityController);
            var defProp = serializedAbility.FindProperty("definition");
            if (defProp != null)
            {
                defProp.objectReferenceValue = abilityDef;
                serializedAbility.ApplyModifiedProperties();
            }
            abilityController.Initialize(abilityDef, eventBus, generalTransform: generalGo.transform);

            // HUD
            var canvasGo = new GameObject("Canvas");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
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
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -80f);
                rect.sizeDelta = new Vector2(400f, 100f);
            }

            var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.fontSize = 54;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.text = "Tropa: 10";

            var serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("countText").objectReferenceValue = tmp;
            serializedHud.FindProperty("squadController").objectReferenceValue = squad;
            serializedHud.ApplyModifiedProperties();
            hud.SetCountText(tmp);
            hud.Initialize(eventBus, squad.SquadCount);

            // GeneralAbilityHud
            var abilityHudGo = new GameObject("GeneralAbilityHud", typeof(RectTransform));
            abilityHudGo.transform.SetParent(canvasGo.transform, false);
            var abilityHudRect = abilityHudGo.GetComponent<RectTransform>();
            if (abilityHudRect != null)
            {
                abilityHudRect.anchorMin = abilityHudRect.anchorMax = abilityHudRect.pivot = new Vector2(0.5f, 0f);
                abilityHudRect.anchoredPosition = new Vector2(0f, 160f);
                abilityHudRect.sizeDelta = new Vector2(500f, 220f);
            }
            var abilityHud = abilityHudGo.AddComponent<GeneralAbilityHud>();

            var sliderGo = new GameObject("ProgressSlider", typeof(RectTransform));
            sliderGo.transform.SetParent(abilityHudGo.transform, false);
            var sliderRect = sliderGo.GetComponent<RectTransform>();
            if (sliderRect != null)
            {
                sliderRect.anchorMin = sliderRect.anchorMax = sliderRect.pivot = new Vector2(0.5f, 0.5f);
                sliderRect.anchoredPosition = new Vector2(0f, 40f);
                sliderRect.sizeDelta = new Vector2(300f, 20f);
            }
            var slider = sliderGo.AddComponent<UnityEngine.UI.Slider>();
            slider.minValue = 0f;
            slider.maxValue = 25f;
            slider.value = 0f;

            var chargesTextGo = new GameObject("ChargesText", typeof(RectTransform));
            chargesTextGo.transform.SetParent(abilityHudGo.transform, false);
            var chargesRect = chargesTextGo.GetComponent<RectTransform>();
            if (chargesRect != null)
            {
                chargesRect.anchorMin = chargesRect.anchorMax = chargesRect.pivot = new Vector2(0.5f, 0.5f);
                chargesRect.anchoredPosition = new Vector2(0f, 75f);
                chargesRect.sizeDelta = new Vector2(300f, 40f);
            }
            var chargesTmp = chargesTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            chargesTmp.fontSize = 28;
            chargesTmp.fontStyle = TMPro.FontStyles.Bold;
            chargesTmp.alignment = TMPro.TextAlignmentOptions.Center;
            chargesTmp.color = Color.white;
            chargesTmp.text = "Cargas: 0";

            var manualBtnGo = new GameObject("ManualTriggerButton", typeof(RectTransform));
            manualBtnGo.transform.SetParent(abilityHudGo.transform, false);
            var manualBtnRect = manualBtnGo.GetComponent<RectTransform>();
            if (manualBtnRect != null)
            {
                manualBtnRect.anchorMin = manualBtnRect.anchorMax = manualBtnRect.pivot = new Vector2(0.5f, 0.5f);
                manualBtnRect.anchoredPosition = new Vector2(-80f, -20f);
                manualBtnRect.sizeDelta = new Vector2(140f, 50f);
            }
            manualBtnGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.9f, 0.3f, 0.2f);
            var manualBtn = manualBtnGo.AddComponent<UnityEngine.UI.Button>();
            manualBtn.interactable = false;

            var manualBtnTextGo = new GameObject("Text", typeof(RectTransform));
            manualBtnTextGo.transform.SetParent(manualBtnGo.transform, false);
            var manualBtnTmp = manualBtnTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            manualBtnTmp.fontSize = 22;
            manualBtnTmp.alignment = TMPro.TextAlignmentOptions.Center;
            manualBtnTmp.color = Color.white;
            manualBtnTmp.text = "Disparar";

            var modeBtnGo = new GameObject("ModeToggleButton", typeof(RectTransform));
            modeBtnGo.transform.SetParent(abilityHudGo.transform, false);
            var modeBtnRect = modeBtnGo.GetComponent<RectTransform>();
            if (modeBtnRect != null)
            {
                modeBtnRect.anchorMin = modeBtnRect.anchorMax = modeBtnRect.pivot = new Vector2(0.5f, 0.5f);
                modeBtnRect.anchoredPosition = new Vector2(80f, -20f);
                modeBtnRect.sizeDelta = new Vector2(140f, 50f);
            }
            modeBtnGo.AddComponent<UnityEngine.UI.Image>().color = new Color(0.2f, 0.6f, 0.9f);
            var modeBtn = modeBtnGo.AddComponent<UnityEngine.UI.Button>();

            var modeTextGo = new GameObject("ModeText", typeof(RectTransform));
            modeTextGo.transform.SetParent(modeBtnGo.transform, false);
            var modeTmp = modeTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            modeTmp.fontSize = 22;
            modeTmp.alignment = TMPro.TextAlignmentOptions.Center;
            modeTmp.color = Color.white;
            modeTmp.text = "Auto";

            var serializedAbilityHud = new SerializedObject(abilityHud);
            serializedAbilityHud.FindProperty("controller").objectReferenceValue = abilityController;
            serializedAbilityHud.FindProperty("progressSlider").objectReferenceValue = slider;
            serializedAbilityHud.FindProperty("chargesText").objectReferenceValue = chargesTmp;
            serializedAbilityHud.FindProperty("manualTriggerButton").objectReferenceValue = manualBtn;
            serializedAbilityHud.FindProperty("modeToggleButton").objectReferenceValue = modeBtn;
            serializedAbilityHud.FindProperty("modeText").objectReferenceValue = modeTmp;
            serializedAbilityHud.ApplyModifiedProperties();

            // AbilityAimIndicator
            var aimIndicatorGo = new GameObject("AbilityAimIndicator");
            var aimIndicator = aimIndicatorGo.AddComponent<AbilityAimIndicator>();
            aimIndicator.Initialize();
            abilityHud.AimIndicator = aimIndicator;

            abilityHud.ConfigureComponents(slider, chargesTmp, manualBtn, modeBtn, modeTmp);
            abilityHud.Initialize(abilityController, eventBus);

            // FollowCamera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.transform.rotation = Quaternion.Euler(30f, 0f, 0f);

            var followCam = camGo.AddComponent<FollowCamera>();
            followCam.Target = generalGo.transform;
            followCam.Offset = new Vector3(0f, 7.0f, -9.0f);
            followCam.Snap();

            // Gates
            var gatesRoot = new GameObject("Gates");
            gatesRoot.layer = CollisionLayers.PickupLayer;
            for (int i = 0; i < gatePairs.Count; i++)
            {
                var spec = gatePairs[i];
                CreateGatePair(gatesRoot.transform, spec.Z, i + 1, LoadPerk(spec.Lane0PerkId), LoadPerk(spec.Lane1PerkId), litShader);
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[Game.Editor.SceneBuilder] {sceneLabel} Greybox scene built successfully at {scenePath}.");
        }

        public static void BuildM4GreyboxSceneCli()
        {
            BuildM4GreyboxScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Horde Runner/Scenes/Build M5 Greybox Scene")]
        public static void BuildM5GreyboxScene()
        {
            BuildCombatGreyboxScene(
                M5GreyboxScenePath,
                "M5",
                new[]
                {
                    new GatePairSpec(25f, "weapon_shotgun", "weapon_smg"),
                    new GatePairSpec(60f, "damage_up_25", "fire_rate_up_1"),
                    new GatePairSpec(95f, "add_10", "multiply_2"),
                },
                initialWeaponId: "pistol",
                loadCatalog: LoadWeaponCatalog);
        }

        public static void BuildM5GreyboxSceneCli()
        {
            BuildM5GreyboxScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        [MenuItem("Horde Runner/Scenes/Build M6 Greybox Scene")]
        public static void BuildM6GreyboxScene()
        {
            BuildCombatGreyboxScene(
                M6GreyboxScenePath,
                "M6",
                new[]
                {
                    new GatePairSpec(25f, "ammo_cryo", "ammo_fire"),
                    new GatePairSpec(60f, "damage_up_25", "ammo_toxic"),
                    new GatePairSpec(95f, "ammo_shock", "multiply_2"),
                },
                initialWeaponId: "pistol",
                loadCatalog: LoadWeaponCatalog,
                enemyHealth: 40,
                setupEnemyStatus: (template, ctrl) =>
                {
                    var catalog = LoadStatusCatalog();
                    var interactions = LoadInteractionTable();

                    var directorGo = new GameObject("StatusEffectDirector");
                    var director = directorGo.AddComponent<StatusEffectDirector>();
                    director.Initialize(catalog, interactions);

                    var serializedDirector = new SerializedObject(director);
                    serializedDirector.FindProperty("catalog").objectReferenceValue = catalog;
                    serializedDirector.FindProperty("interactions").objectReferenceValue = interactions;
                    serializedDirector.ApplyModifiedProperties();

                    var serializedCtrl = new SerializedObject(ctrl);
                    serializedCtrl.FindProperty("statusDirector").objectReferenceValue = director;
                    serializedCtrl.ApplyModifiedProperties();

                    ctrl.AttachStatusDirector(director);

                    var tint = template.AddComponent<EnemyStatusTint>();
                    var serializedTint = new SerializedObject(tint);
                    serializedTint.FindProperty("enemy").objectReferenceValue = ctrl;
                    serializedTint.FindProperty("targetRenderer").objectReferenceValue = template.GetComponent<Renderer>();
                    serializedTint.ApplyModifiedProperties();
                });
        }

        public static void BuildM6GreyboxSceneCli()
        {
            BuildM6GreyboxScene();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static StatusCatalog LoadStatusCatalog()
        {
            var catalog = StatusContentImporter.LoadStatusCatalog();
            if (catalog == null)
            {
                StatusContentImporter.ImportStatuses();
                catalog = StatusContentImporter.LoadStatusCatalog();
            }

            if (catalog == null)
            {
                throw new System.InvalidOperationException(
                    $"[Game.Editor.SceneBuilder] StatusCatalog not found under {StatusContentImporter.DefaultTargetPath}; run tools/unity import-content.");
            }

            return catalog;
        }

        private static EffectInteractionTable LoadInteractionTable()
        {
            var table = StatusContentImporter.LoadInteractionTable();
            if (table == null)
            {
                StatusContentImporter.ImportInteractions();
                table = StatusContentImporter.LoadInteractionTable();
            }

            if (table == null)
            {
                throw new System.InvalidOperationException(
                    $"[Game.Editor.SceneBuilder] EffectInteractionTable not found under {StatusContentImporter.DefaultTargetPath}; run tools/unity import-content.");
            }

            return table;
        }

        // Sem catálogo o WeaponController não equipa nada e todo portão de arma vira no-op silencioso.
        private static WeaponCatalog LoadWeaponCatalog()
        {
            var catalog = WeaponImporter.LoadCatalog();
            if (catalog == null)
            {
                WeaponImporter.ImportAll();
                catalog = WeaponImporter.LoadCatalog();
            }

            if (catalog == null)
            {
                throw new System.InvalidOperationException(
                    $"[Game.Editor.SceneBuilder] WeaponCatalog not found under {WeaponImporter.DefaultTargetPath}; run tools/unity import-content.");
            }

            return catalog;
        }

        private static PerkDefinition LoadPerk(string id)
        {
            string path = $"Assets/_Game/Data/Perks/{id}.asset";
            var perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>(path);
            if (perk == null)
            {
                PerkImporter.ImportAll();
                perk = AssetDatabase.LoadAssetAtPath<PerkDefinition>(path);
            }

            return perk;
        }

        private static GatePair CreateGatePair(
            Transform parent,
            float zPosition,
            int pairIndex,
            PerkDefinition lane0Perk,
            PerkDefinition lane1Perk,
            Shader shader)
        {
            var pairGo = new GameObject($"GatePair_{pairIndex}");
            pairGo.layer = CollisionLayers.PickupLayer;
            pairGo.transform.SetParent(parent, false);
            pairGo.transform.position = new Vector3(0f, 0f, zPosition);
            var pair = pairGo.AddComponent<GatePair>();

            var gate0 = CreateSingleGate(pairGo.transform, 0, -1.0f, lane0Perk, pair, shader);
            var gate1 = CreateSingleGate(pairGo.transform, 1, 1.0f, lane1Perk, pair, shader);

            pair.Initialize(new[] { gate0, gate1 });

            var serializedPair = new SerializedObject(pair);
            var gatesProp = serializedPair.FindProperty("gates");
            if (gatesProp != null)
            {
                gatesProp.ClearArray();
                for (int i = 0; i < 2; i++)
                {
                    gatesProp.InsertArrayElementAtIndex(i);
                    gatesProp.GetArrayElementAtIndex(i).objectReferenceValue = i == 0 ? gate0 : gate1;
                }
                serializedPair.ApplyModifiedProperties();
            }

            return pair;
        }

        private static Gate CreateSingleGate(
            Transform parent,
            int laneIndex,
            float xPosition,
            PerkDefinition perk,
            GatePair parentPair,
            Shader shader)
        {
            var gateGo = new GameObject($"Gate_Lane_{laneIndex}");
            gateGo.layer = CollisionLayers.PickupLayer;
            gateGo.transform.SetParent(parent, false);
            gateGo.transform.localPosition = new Vector3(xPosition, 1.25f, 0f);

            var collider = gateGo.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.8f, 2.5f, 0.4f);

            var rb = gateGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(laneIndex, perk, parentPair);

            var serializedGate = new SerializedObject(gate);
            serializedGate.FindProperty("laneIndex").intValue = laneIndex;
            serializedGate.FindProperty("perk").objectReferenceValue = perk;
            serializedGate.FindProperty("parentPair").objectReferenceValue = parentPair;
            serializedGate.ApplyModifiedProperties();

            var frameGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameGo.name = "Frame";
            frameGo.transform.SetParent(gateGo.transform, false);
            frameGo.transform.localScale = new Vector3(1.9f, 2.6f, 0.15f);
            Object.DestroyImmediate(frameGo.GetComponent<Collider>());
            var frameRenderer = frameGo.GetComponent<Renderer>();
            if (frameRenderer != null && shader != null)
            {
                frameRenderer.sharedMaterial = new Material(shader) { color = Color.white };
            }

            var panelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelGo.name = "Panel";
            panelGo.transform.SetParent(gateGo.transform, false);
            panelGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            panelGo.transform.localScale = new Vector3(1.6f, 2.3f, 0.08f);
            Object.DestroyImmediate(panelGo.GetComponent<Collider>());
            var panelRenderer = panelGo.GetComponent<Renderer>();
            if (panelRenderer != null && shader != null)
            {
                panelRenderer.sharedMaterial = new Material(shader) { color = Color.white };
            }

            var textGo = new GameObject("LabelText");
            textGo.transform.SetParent(gateGo.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.2f, -0.1f);
            var tmp = textGo.AddComponent<TMPro.TextMeshPro>();
            tmp.fontSize = 8;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);

            var gateView = gateGo.AddComponent<GateView>();
            gateView.LabelText = tmp;
            gateView.PanelRenderer = panelRenderer;
            gateView.FrameRenderer = frameRenderer;

            var serializedView = new SerializedObject(gateView);
            serializedView.FindProperty("labelText").objectReferenceValue = tmp;
            serializedView.FindProperty("panelRenderer").objectReferenceValue = panelRenderer;
            serializedView.FindProperty("frameRenderer").objectReferenceValue = frameRenderer;
            serializedView.ApplyModifiedProperties();

            gateView.Initialize(perk, gate);

            return gate;
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
