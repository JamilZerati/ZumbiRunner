#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Holdout;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class VisualRegressionPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private Scene _loadedScene;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.Destroy(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();

            if (_loadedScene.IsValid() && _loadedScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_loadedScene);
            }
        }

        private IEnumerator LoadM6FromDisk()
        {
            var operation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                M6ScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            while (!operation.isDone)
            {
                yield return null;
            }

            _loadedScene = SceneManager.GetSceneByPath(M6ScenePath);
            SceneManager.SetActiveScene(_loadedScene);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Holdout_VisualIntegrity_RendersBarricadeAndHudCorrectly()
        {
            yield return LoadM6FromDisk();

            var cam = Camera.main;
            Assert.IsNotNull(cam, "Camera principal deve existir na cena.");

            var def = ScriptableObject.CreateInstance<HoldoutDefinition>();
            def.SetData("visual_test", "Barricada Visual", 150, 5, 10f, 20f, 2f, 1);

            var barricadeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _spawnedObjects.Add(barricadeGo);
            barricadeGo.name = "VisualTestBarricade";
            barricadeGo.transform.position = new Vector3(0f, 0.6f, 15f);
            barricadeGo.transform.localScale = new Vector3(4.2f, 1.2f, 0.5f);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var barRenderer = barricadeGo.GetComponent<Renderer>();
            if (barRenderer != null && litShader != null)
            {
                barRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.95f, 0.55f, 0.1f) };
            }

            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Definition = def;
            barricade.EnsurePhysicsSetup();

            var barTextGo = new GameObject("VisualBarricadeText");
            _spawnedObjects.Add(barTextGo);
            barTextGo.transform.SetParent(barricadeGo.transform, false);
            barTextGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var barTmp = barTextGo.AddComponent<TextMeshPro>();
            barTmp.fontSize = 7;
            barTmp.alignment = TextAlignmentOptions.Center;
            barTmp.color = Color.yellow;
            barTmp.text = "BARRICADA 150/150";

            var barView = barricadeGo.AddComponent<BarricadeView>();
            barView.HpText = barTmp;
            barView.Bind(barricade);

            yield return null;

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Ignore("Ambiente -nographics detectado sem dispositivo grafico: teste visual requer GPU/batchmode grafico (disponivel via tools/unity record-gameplay).");
                yield break;
            }

            int w = 270;
            int h = 480;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            var pixels = tex.GetPixels();
            float nonBlackLuminanceSum = 0f;
            int nonBlackPixels = 0;
            int warmPixels = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                if (lum > 0.05f)
                {
                    nonBlackPixels++;
                    nonBlackLuminanceSum += lum;
                }
                if (c.r > 0.6f && c.g > 0.3f && c.b < 0.3f)
                {
                    warmPixels++;
                }
            }

            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(def);

            float visibleRatio = (float)nonBlackPixels / pixels.Length;
            Assert.Greater(visibleRatio, 0.40f, "Viewport renderizada deve conter geometria e iluminacao visiveis (> 40% da area).");
            Assert.Greater(warmPixels, 20, "Barricada laranja deve ser visivel na projecao de camera em frente ao General.");
        }

        [UnityTest]
        public IEnumerator RecordGameplayVideo()
        {
            yield return LoadM6FromDisk();

            var cam = Camera.main;
            var generalGo = GameObject.Find("General");
            var scroller = generalGo != null ? generalGo.GetComponent<TrackScroller>() : null;
            var squad = generalGo != null ? generalGo.GetComponent<SquadController>() : null;
            var mover = generalGo != null ? generalGo.GetComponent<LaneMover>() : null;
            var squadVisual = Object.FindFirstObjectByType<SquadVisualController>();

            var def = ScriptableObject.CreateInstance<HoldoutDefinition>();
            def.SetData("holdout_recorded", "Barricada Setor 4", 150, 5, 5f, 25f, 2f, 1);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            var barricadeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _spawnedObjects.Add(barricadeGo);
            barricadeGo.name = "HoldoutBarricade";
            barricadeGo.transform.position = new Vector3(0f, 0.6f, 16f);
            barricadeGo.transform.localScale = new Vector3(4.2f, 1.2f, 0.5f);
            var barRenderer = barricadeGo.GetComponent<Renderer>();
            if (barRenderer != null && litShader != null)
            {
                barRenderer.sharedMaterial = new Material(litShader) { color = new Color(0.95f, 0.55f, 0.1f) };
            }

            var barricade = barricadeGo.AddComponent<Barricade>();
            barricade.Definition = def;
            barricade.EnsurePhysicsSetup();

            var barTextGo = new GameObject("BarricadeWorldText");
            _spawnedObjects.Add(barTextGo);
            barTextGo.transform.SetParent(barricadeGo.transform, false);
            barTextGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            var barTmp = barTextGo.AddComponent<TextMeshPro>();
            barTmp.fontSize = 7;
            barTmp.alignment = TextAlignmentOptions.Center;
            barTmp.color = Color.yellow;
            barTmp.text = "BARRICADA: 150/150 HP";

            var barView = barricadeGo.AddComponent<BarricadeView>();
            barView.HpText = barTmp;
            barView.Bind(barricade);

            var controllerGo = new GameObject("HoldoutController");
            _spawnedObjects.Add(controllerGo);
            var controller = controllerGo.AddComponent<HoldoutController>();
            controller.Initialize(def, barricade, scroller, squad, mover, null);

            Time.captureFramerate = 24;

            int w = 540;
            int h = 960;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;

            string dir = ".artifacts/frames_recorded";
            if (System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.Delete(dir, true);
            }
            System.IO.Directory.CreateDirectory(dir);

            int totalFrames = 120;
            bool holdoutStarted = false;

            for (int f = 0; f < totalFrames; f++)
            {
                if (f == 20 && !holdoutStarted)
                {
                    holdoutStarted = true;
                    controller.StartHoldout();
                }
                else if (f == 60 && mover != null)
                {
                    mover.RequestMove(1);
                    barricade.SetGuardedLane(1);
                }
                else if (f == 90 && controller.IsActive)
                {
                    controller.Tick(10f);
                    if (squadVisual != null && squad != null)
                    {
                        squadVisual.SynchronizeSquad(squad.SquadCount);
                    }
                }

                yield return null;

                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                System.IO.File.WriteAllBytes(string.Format("{0}/frame_{1:D4}.png", dir, f), tex.EncodeToPNG());
            }

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Time.captureFramerate = 0;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(def);
        }
    }
}
#endif
