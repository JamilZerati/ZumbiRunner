#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.Events;
using Game.Data;
using Game.Data.Mutations;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Infrastructure;
using Game.Infrastructure.Input;
using Game.Presentation;
using Game.Presentation.UI;
using TMPro;
using UnityEngine.UI;
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
        public const string GrenadeIconPath = "Assets/_Game/Art/UI/grenade_icon.png";
        public const string GrenadeExplosionClipPath = "Assets/_Game/Audio/SFX/grenade_explosion.ogg";
        public const string GrenadeExplosionSpritePath = "Assets/_Game/Art/VFX/grenade_explosion.png";

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

            var canvasGo = new GameObject("Canvas");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            var (uiManager, pauseController) = BuildUiHierarchy(canvasGo);

            var composerGo = new GameObject("GreyboxRunComposer");
            var composer = composerGo.AddComponent<GreyboxRunComposer>();
            composer.ConfigureUi(uiManager, pauseController);
            var serializedComposer = new SerializedObject(composer);
            serializedComposer.FindProperty("uiManager").objectReferenceValue = uiManager;
            serializedComposer.FindProperty("pauseController").objectReferenceValue = pauseController;
            var mutationCatalogAsset = AssetDatabase.LoadAssetAtPath<MutationCatalog>("Assets/_Game/Data/Mutations/MutationCatalog.asset");
            if (mutationCatalogAsset != null)
            {
                serializedComposer.FindProperty("mutationCatalog").objectReferenceValue = mutationCatalogAsset;
                composer.ConfigureMutations(mutationCatalogAsset);
            }
            serializedComposer.ApplyModifiedProperties();

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
            var stateMachine = new GameStateMachine(eventBus, GameState.Boot);
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

            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

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

            var sliderBgGo = new GameObject("Background", typeof(RectTransform));
            sliderBgGo.transform.SetParent(sliderGo.transform, false);
            var sliderBgRect = sliderBgGo.GetComponent<RectTransform>();
            sliderBgRect.anchorMin = Vector2.zero;
            sliderBgRect.anchorMax = Vector2.one;
            sliderBgRect.sizeDelta = Vector2.zero;
            var sliderBgImage = sliderBgGo.AddComponent<UnityEngine.UI.Image>();
            sliderBgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
            sliderBgImage.raycastTarget = false;

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRect = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fillImage = fillGo.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = new Color(0.95f, 0.65f, 0.15f, 1f);
            fillImage.raycastTarget = false;

            var slider = sliderGo.AddComponent<UnityEngine.UI.Slider>();
            slider.interactable = false;
            slider.transition = UnityEngine.UI.Selectable.Transition.None;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
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
                manualBtnRect.anchoredPosition = new Vector2(-90f, -45f);
                manualBtnRect.sizeDelta = new Vector2(110f, 110f);
            }
            var manualBtnImage = manualBtnGo.AddComponent<UnityEngine.UI.Image>();
            manualBtnImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrenadeIconPath);
            manualBtnImage.preserveAspect = true;
            manualBtnImage.color = Color.white;
            var manualBtn = manualBtnGo.AddComponent<UnityEngine.UI.Button>();
            manualBtn.targetGraphic = manualBtnImage;
            manualBtn.interactable = false;

            var manualBtnTextGo = new GameObject("Text", typeof(RectTransform));
            manualBtnTextGo.transform.SetParent(manualBtnGo.transform, false);
            var manualBtnTextRect = manualBtnTextGo.GetComponent<RectTransform>();
            manualBtnTextRect.anchorMin = manualBtnTextRect.anchorMax = manualBtnTextRect.pivot = new Vector2(0.5f, 0.5f);
            manualBtnTextRect.anchoredPosition = new Vector2(0f, -70f);
            manualBtnTextRect.sizeDelta = new Vector2(160f, 30f);
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

            var aimIndicatorGo = new GameObject("AbilityAimIndicator");
            var aimIndicator = aimIndicatorGo.AddComponent<AbilityAimIndicator>();
            aimIndicator.Initialize();
            abilityHud.AimIndicator = aimIndicator;

            var serializedAbilityHud = new SerializedObject(abilityHud);
            serializedAbilityHud.FindProperty("controller").objectReferenceValue = abilityController;
            serializedAbilityHud.FindProperty("aimIndicator").objectReferenceValue = aimIndicator;
            serializedAbilityHud.FindProperty("progressSlider").objectReferenceValue = slider;
            serializedAbilityHud.FindProperty("chargesText").objectReferenceValue = chargesTmp;
            serializedAbilityHud.FindProperty("manualTriggerButton").objectReferenceValue = manualBtn;
            serializedAbilityHud.FindProperty("modeToggleButton").objectReferenceValue = modeBtn;
            serializedAbilityHud.FindProperty("modeText").objectReferenceValue = modeTmp;
            serializedAbilityHud.ApplyModifiedProperties();

            abilityHud.ConfigureComponents(slider, chargesTmp, manualBtn, modeBtn, modeTmp);
            abilityHud.Initialize(abilityController, eventBus);

            var abilitySource = abilityHudGo.AddComponent<AudioSource>();
            abilitySource.playOnAwake = false;
            abilitySource.spatialBlend = 0f;
            var abilityAudio = abilityHudGo.AddComponent<GeneralAbilityAudio>();
            var explosionClip = AssetDatabase.LoadAssetAtPath<AudioClip>(GrenadeExplosionClipPath);
            var serializedAbilityAudio = new SerializedObject(abilityAudio);
            serializedAbilityAudio.FindProperty("audioSource").objectReferenceValue = abilitySource;
            serializedAbilityAudio.FindProperty("explosionClip").objectReferenceValue = explosionClip;
            serializedAbilityAudio.ApplyModifiedProperties();
            abilityAudio.Initialize(eventBus);

            var vfxGo = new GameObject("GrenadeExplosionVfx");
            var vfxRenderer = vfxGo.AddComponent<SpriteRenderer>();
            vfxRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GrenadeExplosionSpritePath);
            var abilityVfx = vfxGo.AddComponent<GeneralAbilityExplosionVfx>();
            var serializedVfx = new SerializedObject(abilityVfx);
            serializedVfx.FindProperty("spriteRenderer").objectReferenceValue = vfxRenderer;
            serializedVfx.FindProperty("origin").objectReferenceValue = generalGo.transform;
            serializedVfx.ApplyModifiedProperties();
            abilityVfx.Initialize(eventBus);

            // DebugOverlay
            var debugOverlayGo = new GameObject("DebugOverlay", typeof(RectTransform));
            debugOverlayGo.transform.SetParent(canvasGo.transform, false);
            var debugOverlayRect = debugOverlayGo.GetComponent<RectTransform>();
            if (debugOverlayRect != null)
            {
                debugOverlayRect.anchorMin = new Vector2(0f, 1f);
                debugOverlayRect.anchorMax = new Vector2(1f, 1f);
                debugOverlayRect.pivot = new Vector2(0.5f, 1f);
                debugOverlayRect.anchoredPosition = new Vector2(0f, -10f);
                debugOverlayRect.sizeDelta = new Vector2(-40f, 480f);
            }
            var debugOverlay = debugOverlayGo.AddComponent<DebugTextOverlay>();

            var panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(debugOverlayGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;
            }
            var debugBg = panelGo.AddComponent<UnityEngine.UI.Image>();
            debugBg.color = new Color(0f, 0f, 0f, 0.72f);

            var debugTextGo = new GameObject("DebugText", typeof(RectTransform));
            debugTextGo.transform.SetParent(panelGo.transform, false);
            var debugTextRect = debugTextGo.GetComponent<RectTransform>();
            if (debugTextRect != null)
            {
                debugTextRect.anchorMin = Vector2.zero;
                debugTextRect.anchorMax = Vector2.one;
                debugTextRect.pivot = new Vector2(0.5f, 0.5f);
                debugTextRect.offsetMin = new Vector2(20f, 15f);
                debugTextRect.offsetMax = new Vector2(-20f, -15f);
            }
            var debugTmp = debugTextGo.AddComponent<TMPro.TextMeshProUGUI>();
            debugTmp.fontSize = 24;
            debugTmp.fontStyle = TMPro.FontStyles.Normal;
            debugTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            debugTmp.color = Color.white;
            debugTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            debugTmp.text = "[DEBUG OVERLAY]";

            var serializedDebugOverlay = new SerializedObject(debugOverlay);
            serializedDebugOverlay.FindProperty("debugText").objectReferenceValue = debugTmp;
            serializedDebugOverlay.FindProperty("visualRoot").objectReferenceValue = panelGo;
            serializedDebugOverlay.FindProperty("squadController").objectReferenceValue = squad;
            serializedDebugOverlay.FindProperty("combatDirector").objectReferenceValue = director;
            serializedDebugOverlay.FindProperty("trackScroller").objectReferenceValue = scroller;
            serializedDebugOverlay.FindProperty("weaponController").objectReferenceValue = weapon;
            serializedDebugOverlay.FindProperty("abilityController").objectReferenceValue = abilityController;
            serializedDebugOverlay.ApplyModifiedProperties();
            debugOverlay.ConfigureComponents(debugTmp, panelGo);
            debugOverlay.Initialize(eventBus, squad, director, scroller, weapon, abilityController);

            var (uiManager, pauseController) = BuildUiHierarchy(canvasGo);

            var composerGo = new GameObject("GreyboxRunComposer");
            var composer = composerGo.AddComponent<GreyboxRunComposer>();
            var serializedComposer = new SerializedObject(composer);
            serializedComposer.FindProperty("combatDirector").objectReferenceValue = director;
            serializedComposer.FindProperty("hordeSpawner").objectReferenceValue = spawner;
            serializedComposer.FindProperty("statusDirector").objectReferenceValue = Object.FindFirstObjectByType<StatusEffectDirector>();
            serializedComposer.FindProperty("abilityController").objectReferenceValue = abilityController;
            serializedComposer.FindProperty("abilityHud").objectReferenceValue = abilityHud;
            serializedComposer.FindProperty("abilityAudio").objectReferenceValue = abilityAudio;
            serializedComposer.FindProperty("abilityVfx").objectReferenceValue = abilityVfx;
            serializedComposer.FindProperty("debugOverlay").objectReferenceValue = debugOverlay;
            serializedComposer.FindProperty("uiManager").objectReferenceValue = uiManager;
            serializedComposer.FindProperty("pauseController").objectReferenceValue = pauseController;
            var mutationCatalogAsset = AssetDatabase.LoadAssetAtPath<MutationCatalog>("Assets/_Game/Data/Mutations/MutationCatalog.asset");
            if (mutationCatalogAsset != null)
            {
                serializedComposer.FindProperty("mutationCatalog").objectReferenceValue = mutationCatalogAsset;
                composer.ConfigureMutations(mutationCatalogAsset);
            }
            composer.ConfigureUi(uiManager, pauseController);
            serializedComposer.ApplyModifiedProperties();

            // FollowCamera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
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

        public static void CaptureM6ScreenshotCli()
        {
            BuildM6GreyboxScene();
            var scene = EditorSceneManager.OpenScene(M6GreyboxScenePath);

            var cam = Camera.main;
            var overlay = Object.FindFirstObjectByType<DebugTextOverlay>();
            if (cam != null && overlay != null)
            {
                overlay.Refresh();
                Canvas.ForceUpdateCanvases();

                var canvas = overlay.GetComponentInParent<Canvas>();
                var originalMode = canvas.renderMode;
                var originalCam = canvas.worldCamera;
                var originalDist = canvas.planeDistance;

                var rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                var prevTarget = cam.targetTexture;
                var prevActive = RenderTexture.active;

                try
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 2f;
                    Canvas.ForceUpdateCanvases();

                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                    tex.Apply();

                    var path = "docs/evidencias/NEX-781-m6-debug-overlay.png";
                    var dir = System.IO.Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }

                    System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Debug.Log($"[SceneBuilder] Screenshot saved to {path} ({new System.IO.FileInfo(path).Length} bytes).");
                }
                finally
                {
                    cam.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    canvas.renderMode = originalMode;
                    canvas.worldCamera = originalCam;
                    canvas.planeDistance = originalDist;
                    Canvas.ForceUpdateCanvases();
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }

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

        private static (UIManager uiManager, PauseController pauseController) BuildUiHierarchy(GameObject canvasGo)
        {
            var uiManagerGo = new GameObject("UIManager", typeof(RectTransform));
            uiManagerGo.transform.SetParent(canvasGo.transform, false);
            var uiManager = uiManagerGo.AddComponent<UIManager>();

            var hubLayerGo = CreateLayer(uiManagerGo, "HubLayer", UILayerType.Hub, uiManager, out var hubLayer);
            var hudLayerGo = CreateLayer(uiManagerGo, "GameHudLayer", UILayerType.GameHud, uiManager, out var hudLayer);
            var modalLayerGo = CreateLayer(uiManagerGo, "ModalLayer", UILayerType.Modal, uiManager, out var modalLayer);
            var tutorialLayerGo = CreateLayer(uiManagerGo, "TutorialOverlayLayer", UILayerType.TutorialOverlay, uiManager, out var tutorialLayer);

            var hubGo = CreateRect(hubLayerGo, "HubView");
            var hubView = hubGo.AddComponent<HubView>();
            var coinsText = CreateTmp(hubGo, "CoinsText", "Moedas: 100", 32, TextAlignmentOptions.TopRight, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(300, 60));
            var playBtn = CreateBtn(hubGo, "PlayButton", "JOGAR", new Vector2(0.5f, 0.1f), Vector2.zero, new Vector2(320, 80));
            var optionsBtn = CreateBtn(hubGo, "OptionsButton", "Opções", new Vector2(0.15f, 0.95f), Vector2.zero, new Vector2(160, 60));
            var debutBtn = CreateBtn(hubGo, "DebutJourneyButton", "7 Dias", new Vector2(0.35f, 0.95f), Vector2.zero, new Vector2(160, 60));
            var barracksBtn = CreateBtn(hubGo, "BarracksButton", "Quartel", new Vector2(0.15f, 0.05f), Vector2.zero, new Vector2(140, 60));
            var shopBtn = CreateBtn(hubGo, "ShopButton", "Loja", new Vector2(0.35f, 0.05f), Vector2.zero, new Vector2(140, 60));
            var passBtn = CreateBtn(hubGo, "PassButton", "Passe", new Vector2(0.85f, 0.05f), Vector2.zero, new Vector2(140, 60));
            var feedbackText = CreateTmp(hubGo, "FeedbackText", "", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(500, 50));

            var stageWidgetGo = CreateRect(hubGo, "StageSelectionWidget");
            var stageWidget = stageWidgetGo.AddComponent<StageSelectionWidget>();
            var stageTitle = CreateTmp(stageWidgetGo, "StageTitle", "Fase 1", 36, TextAlignmentOptions.Center, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(400, 60));
            var stageStars = CreateTmp(stageWidgetGo, "StageStars", "★★★", 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(200, 40));
            var lockIndicator = CreateRect(stageWidgetGo, "LockIndicator");
            var prevBtn = CreateBtn(stageWidgetGo, "PrevButton", "<", new Vector2(0.5f, 0.6f), new Vector2(-220, 0), new Vector2(60, 60));
            var nextBtn = CreateBtn(stageWidgetGo, "NextButton", ">", new Vector2(0.5f, 0.6f), new Vector2(220, 0), new Vector2(60, 60));
            stageWidget.BindComponents(stageTitle, stageStars, lockIndicator, prevBtn, nextBtn);
            WireSerialized(stageWidget, ("_stageTitleText", stageTitle), ("_starsText", stageStars), ("_lockIndicator", lockIndicator), ("_prevButton", prevBtn), ("_nextButton", nextBtn));
            var mutationWidgetGo = CreateRect(hubGo, "MutationSelectionWidget");
            var mutationWidget = mutationWidgetGo.AddComponent<MutationSelectionWidget>();
            var mutationItemsContainer = CreateRect(mutationWidgetGo, "ItemsContainer");
            var totalBonusText = CreateTmp(mutationWidgetGo, "TotalBonusText", "+0% Moedas", 20, TextAlignmentOptions.Center, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(300, 30));
            var lockedOverlay = CreateRect(mutationWidgetGo, "LockedOverlay");
            var lockedReason = CreateTmp(lockedOverlay, "LockedReason", "Derrote o Boss da Fase 5", 18, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 40));

            var explicitItems = new List<MutationToggleItem>
            {
                CreateMutationToggleItem(mutationItemsContainer, "MutationItem_armored", "Blindados"),
                CreateMutationToggleItem(mutationItemsContainer, "MutationItem_frenzy", "Frenesi"),
                CreateMutationToggleItem(mutationItemsContainer, "MutationItem_fog", "Neblina"),
                CreateMutationToggleItem(mutationItemsContainer, "MutationItem_last_soldier", "Último Soldado"),
                CreateMutationToggleItem(mutationItemsContainer, "MutationItem_no_multiplication", "Sem Multiplicação")
            };

            mutationWidget.BindComponents(mutationItemsContainer.transform, null, totalBonusText, lockedOverlay, lockedReason, explicitItems);
            WireSerialized(mutationWidget, ("_itemsContainer", mutationItemsContainer.transform), ("_totalBonusText", totalBonusText), ("_lockedOverlay", lockedOverlay), ("_lockedReasonText", lockedReason));

            hubView.BindComponents(coinsText, playBtn, optionsBtn, debutBtn, barracksBtn, shopBtn, passBtn, stageWidget, feedbackText, mutationWidget);
            WireSerialized(hubView, ("_coinsText", coinsText), ("_playButton", playBtn), ("_optionsButton", optionsBtn), ("_debutJourneyButton", debutBtn), ("_barracksButton", barracksBtn), ("_shopButton", shopBtn), ("_passButton", passBtn), ("_stageSelectionWidget", stageWidget), ("_navigationFeedbackText", feedbackText), ("_mutationSelectionWidget", mutationWidget));
            uiManager.RegisterWindow(hubView);

            var pauseGo = CreateRect(modalLayerGo, "PauseView");
            var pauseView = pauseGo.AddComponent<PauseView>();
            var resumeBtn = CreateBtn(pauseGo, "ResumeButton", "Retomar", new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(260, 60));
            var restartBtn = CreateBtn(pauseGo, "RestartButton", "Reiniciar", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 60));
            var pauseOptBtn = CreateBtn(pauseGo, "OptionsButton", "Opções", new Vector2(0.5f, 0.4f), Vector2.zero, new Vector2(260, 60));
            var quitBtn = CreateBtn(pauseGo, "QuitButton", "Sair para o Hub", new Vector2(0.5f, 0.3f), Vector2.zero, new Vector2(260, 60));
            pauseView.SetupControls(resumeBtn, restartBtn, pauseOptBtn, quitBtn);
            WireSerialized(pauseView, ("resumeButton", resumeBtn), ("restartButton", restartBtn), ("optionsButton", pauseOptBtn), ("quitButton", quitBtn));
            uiManager.RegisterWindow(pauseView);

            var optionsGo = CreateRect(modalLayerGo, "OptionsView");
            var optionsView = optionsGo.AddComponent<OptionsView>();
            var masterSlider = CreateSlider(optionsGo, "MasterSlider", new Vector2(0.5f, 0.7f), new Vector2(300, 30));
            var sfxSlider = CreateSlider(optionsGo, "SfxSlider", new Vector2(0.5f, 0.6f), new Vector2(300, 30));
            var bgmSlider = CreateSlider(optionsGo, "BgmSlider", new Vector2(0.5f, 0.5f), new Vector2(300, 30));
            var abilityToggle = CreateToggle(optionsGo, "AbilityModeToggle", new Vector2(0.5f, 0.42f), new Vector2(-80, 0), new Vector2(40, 40));
            var abilityBtn = CreateBtn(optionsGo, "AbilityModeButton", "Modo", new Vector2(0.5f, 0.42f), new Vector2(80, 0), new Vector2(120, 50));
            var abilityTxt = CreateTmp(optionsGo, "AbilityModeText", "Auto", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.42f), new Vector2(80, 0), new Vector2(120, 50));
            var dropdown = CreateDropdown(optionsGo, "LanguageDropdown", new Vector2(0.5f, 0.32f), new Vector2(200, 40), new List<string> { "pt-BR", "en-US", "es" });
            var closeOptBtn = CreateBtn(optionsGo, "CloseButton", "Fechar", new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(180, 50));
            optionsView.SetupControls(masterSlider, sfxSlider, bgmSlider, abilityToggle, abilityBtn, abilityTxt, legacyLangDropdown: dropdown, closeBtn: closeOptBtn);
            WireSerialized(optionsView, ("masterVolumeSlider", masterSlider), ("sfxVolumeSlider", sfxSlider), ("bgmVolumeSlider", bgmSlider), ("abilityModeToggle", abilityToggle), ("abilityModeButton", abilityBtn), ("abilityModeText", abilityTxt), ("legacyLanguageDropdown", dropdown), ("closeButton", closeOptBtn));
            uiManager.RegisterWindow(optionsView);

            var pauseControllerGo = new GameObject("PauseController");
            pauseControllerGo.transform.SetParent(canvasGo.transform, false);
            var pauseController = pauseControllerGo.AddComponent<PauseController>();
            WireSerialized(pauseController, ("uiManager", uiManager), ("pauseView", pauseView), ("optionsView", optionsView));

            var resultGo = CreateRect(modalLayerGo, "ResultView");
            var resultView = resultGo.AddComponent<ResultView>();
            var resTitle = CreateTmp(resultGo, "TitleText", "Vitória", 44, TextAlignmentOptions.Center, new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(400, 60));
            var resCoins = CreateTmp(resultGo, "CoinsText", "Moedas: 100", 36, TextAlignmentOptions.Center, new Vector2(0.5f, 0.65f), Vector2.zero, new Vector2(300, 50));
            var resMult = CreateTmp(resultGo, "MultiplierText", "x2", 30, TextAlignmentOptions.Center, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(200, 40));
            var resStars = CreateTmp(resultGo, "StarsText", "★★★", 32, TextAlignmentOptions.Center, new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(200, 40));
            var doubleBtn = CreateBtn(resultGo, "DoubleRewardButton", "Dobrar Moedas (Ad)", new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(300, 70));
            var continueBtn = CreateBtn(resultGo, "ContinueButton", "Continuar", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(260, 60));
            resultView.BindComponents(resTitle, resCoins, resMult, resStars, doubleBtn, continueBtn);
            WireSerialized(resultView, ("_titleText", resTitle), ("_coinsText", resCoins), ("_multiplierText", resMult), ("_starsText", resStars), ("_doubleRewardButton", doubleBtn), ("_continueButton", continueBtn));
            uiManager.RegisterWindow(resultView);

            var debutGo = CreateRect(modalLayerGo, "DebutJourneyView");
            var debutView = debutGo.AddComponent<DebutJourneyView>();
            var debTitle = CreateTmp(debutGo, "TitleText", "Jornada de Estreia (7 Dias)", 36, TextAlignmentOptions.Center, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(500, 60));
            var debFeedback = CreateTmp(debutGo, "FeedbackText", "", 24, TextAlignmentOptions.Center, new Vector2(0.5f, 0.2f), Vector2.zero, new Vector2(400, 40));
            var debClaimBtn = CreateBtn(debutGo, "ClaimButton", "Resgatar Dia", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(240, 60));
            var debCloseBtn = CreateBtn(debutGo, "CloseButton", "Fechar", new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(200, 50));
            var dayTexts = new TMP_Text[7];
            for (int i = 0; i < 7; i++)
            {
                dayTexts[i] = CreateTmp(debutGo, $"Day_{i + 1}_Text", $"Dia {i + 1}: Bloqueado", 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.75f - (i * 0.06f)), Vector2.zero, new Vector2(350, 30));
            }
            debutView.BindComponents(debCloseBtn, debClaimBtn, dayTexts, debTitle, debFeedback);
            WireSerialized(debutView, ("_titleText", debTitle), ("_feedbackText", debFeedback), ("_claimButton", debClaimBtn), ("_closeButton", debCloseBtn));
            var serializedDebut = new SerializedObject(debutView);
            var dayProp = serializedDebut.FindProperty("_dayStatusTexts");
            if (dayProp != null)
            {
                dayProp.arraySize = 7;
                for (int i = 0; i < 7; i++) dayProp.GetArrayElementAtIndex(i).objectReferenceValue = dayTexts[i];
                serializedDebut.ApplyModifiedProperties();
            }
            uiManager.RegisterWindow(debutView);

            var tutGo = CreateRect(tutorialLayerGo, "TutorialGuideView");
            var tutView = tutGo.AddComponent<TutorialGuideView>();
            var bannerRoot = CreateRect(tutGo, "BannerRoot");
            var bannerRect = bannerRoot.GetComponent<RectTransform>();
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 0.8f);
            bannerRect.sizeDelta = new Vector2(600, 100);
            var tutText = CreateTmp(bannerRoot, "InstructionText", "Arraste para mover", 28, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(550, 80));
            var iconGo = CreateRect(bannerRoot, "StepIcon");
            var stepIcon = iconGo.AddComponent<Image>();
            tutView.BindComponents(tutText, bannerRoot, stepIcon);
            WireSerialized(tutView, ("_instructionText", tutText), ("_bannerRoot", bannerRoot), ("_stepIcon", stepIcon));
            uiManager.RegisterWindow(tutView);

            WireLayerType(hubView, UILayerType.Hub);
            WireLayerType(pauseView, UILayerType.Modal);
            WireLayerType(optionsView, UILayerType.Modal);
            WireLayerType(resultView, UILayerType.Modal);
            WireLayerType(debutView, UILayerType.Modal);
            WireLayerType(tutView, UILayerType.TutorialOverlay);

            var soUi = new SerializedObject(uiManager);
            var layersProp = soUi.FindProperty("_layers");
            layersProp.arraySize = 4;
            layersProp.GetArrayElementAtIndex(0).objectReferenceValue = hubLayer;
            layersProp.GetArrayElementAtIndex(1).objectReferenceValue = hudLayer;
            layersProp.GetArrayElementAtIndex(2).objectReferenceValue = modalLayer;
            layersProp.GetArrayElementAtIndex(3).objectReferenceValue = tutorialLayer;

            var windowsProp = soUi.FindProperty("_preloadedWindows");
            windowsProp.arraySize = 6;
            windowsProp.GetArrayElementAtIndex(0).objectReferenceValue = hubView;
            windowsProp.GetArrayElementAtIndex(1).objectReferenceValue = pauseView;
            windowsProp.GetArrayElementAtIndex(2).objectReferenceValue = optionsView;
            windowsProp.GetArrayElementAtIndex(3).objectReferenceValue = resultView;
            windowsProp.GetArrayElementAtIndex(4).objectReferenceValue = debutView;
            windowsProp.GetArrayElementAtIndex(5).objectReferenceValue = tutView;
            soUi.ApplyModifiedProperties();

            return (uiManager, pauseController);
        }

        private static GameObject CreateRect(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return go;
        }

        private static GameObject CreateLayer(GameObject parent, string name, UILayerType layerType, UIManager manager, out UILayer layer)
        {
            var go = CreateRect(parent, name);
            layer = go.AddComponent<UILayer>();
            layer.Initialize(layerType);
            var so = new SerializedObject(layer);
            var prop = so.FindProperty("_layerType");
            if (prop != null)
            {
                prop.enumValueIndex = (int)layerType;
            }
            else
            {
                Debug.LogError($"[SceneBuilder] Propriedade '_layerType' não encontrada na camada '{layer.name}' ({layer.GetType().Name}).");
            }
            so.ApplyModifiedProperties();
            manager.RegisterLayer(layer);
            return go;
        }

        private static void WireLayerType(UIWindow window, UILayerType layerType)
        {
            var so = new SerializedObject(window);
            var prop = so.FindProperty("_layerType");
            if (prop != null)
            {
                prop.enumValueIndex = (int)layerType;
            }
            else
            {
                Debug.LogError($"[SceneBuilder] Propriedade '_layerType' não encontrada na janela '{window.name}' ({window.GetType().Name}).");
            }
            so.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI CreateTmp(GameObject parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateBtn(GameObject parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            CreateTmp(go, "Text", label, 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            return btn;
        }

        private static Slider CreateSlider(GameObject parent, string name, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static Toggle CreateToggle(GameObject parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = true;
            return toggle;
        }

        private static MutationToggleItem CreateMutationToggleItem(GameObject parent, string name, string label)
        {
            var itemGo = CreateRect(parent, name);
            var item = itemGo.AddComponent<MutationToggleItem>();
            var iconGo = CreateRect(itemGo, "Icon");
            var iconImg = iconGo.AddComponent<Image>();
            var nameTxt = CreateTmp(itemGo, "Name", label, 18, TextAlignmentOptions.Left, new Vector2(0.3f, 0.5f), Vector2.zero, new Vector2(140, 30));
            var bonusTxt = CreateTmp(itemGo, "Bonus", "+50%", 16, TextAlignmentOptions.Right, new Vector2(0.8f, 0.5f), Vector2.zero, new Vector2(80, 30));
            var btnGo = CreateRect(itemGo, "Button");
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var indicator = CreateRect(itemGo, "Indicator");
            indicator.SetActive(false);
            item.BindComponents(iconImg, nameTxt, bonusTxt, btn, indicator);
            WireSerialized(item, ("_iconImage", iconImg), ("_nameText", nameTxt), ("_bonusText", bonusTxt), ("_toggleButton", btn), ("_selectedIndicator", indicator));
            return item;
        }

        private static Dropdown CreateDropdown(GameObject parent, string name, Vector2 anchor, Vector2 size, List<string> options)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = size;
            var dropdown = go.AddComponent<Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(options);
            return dropdown;
        }

        private static void WireSerialized(Object target, params (string propName, Object value)[] bindings)
        {
            var so = new SerializedObject(target);
            foreach (var (propName, value) in bindings)
            {
                var prop = so.FindProperty(propName);
                if (prop != null)
                {
                    prop.objectReferenceValue = value;
                }
                else
                {
                    Debug.LogError($"[SceneBuilder] Propriedade serializada '{propName}' não encontrada no alvo '{target.name}' ({target.GetType().Name}).");
                }
            }
            so.ApplyModifiedProperties();
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
