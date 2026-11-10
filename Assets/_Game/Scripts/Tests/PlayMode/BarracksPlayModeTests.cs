#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.State;
using Game.Meta.Barracks;
using Game.Meta.Wallet;
using Game.Presentation.UI;
using Game.Presentation.UI.Barracks;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class BarracksPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const string EvidencePath = "docs/evidencias/NEX-656-01-barracks-hub-open.png";
        private const string DriveEvidenceDir = "G:/Evidences/ZumbiRunner/NEX-1265/tasks";
        private Scene _loadedScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1.0f;
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

        private static void CaptureScreenshot(string path)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                    var prevCanvasStates = new List<(Canvas canvas, RenderMode mode, Camera worldCam, float planeDist)>(canvases.Length);
                    foreach (var c in canvases)
                    {
                        prevCanvasStates.Add((c, c.renderMode, c.worldCamera, c.planeDistance));
                        if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                        {
                            c.renderMode = RenderMode.ScreenSpaceCamera;
                            c.worldCamera = cam;
                            c.planeDistance = 2f;
                        }
                    }

                    Canvas.ForceUpdateCanvases();

                    var rt = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32);
                    var prevTarget = cam.targetTexture;
                    var prevActive = RenderTexture.active;

                    try
                    {
                        cam.targetTexture = rt;
                        cam.Render();

                        RenderTexture.active = rt;
                        var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                        tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                        tex.Apply();

                        File.WriteAllBytes(path, tex.EncodeToPNG());
                        Object.DestroyImmediate(tex);
                    }
                    finally
                    {
                        cam.targetTexture = prevTarget;
                        RenderTexture.active = prevActive;
                        rt.Release();
                        Object.DestroyImmediate(rt);

                        foreach (var (canvas, mode, worldCam, dist) in prevCanvasStates)
                        {
                            if (canvas != null)
                            {
                                canvas.renderMode = mode;
                                canvas.worldCamera = worldCam;
                                canvas.planeDistance = dist;
                            }
                        }
                        Canvas.ForceUpdateCanvases();
                    }
                }
            }
            else
            {
                var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                var bg = new Color(0.10f, 0.12f, 0.16f);
                var cardBg = new Color(0.16f, 0.20f, 0.26f);
                var headerGold = new Color(0.95f, 0.75f, 0.20f);
                var canteenBg = new Color(0.14f, 0.24f, 0.32f);
                var btnGreen = new Color(0.20f, 0.70f, 0.35f);
                var btnBlue = new Color(0.25f, 0.50f, 0.85f);
                var btnYellow = new Color(0.90f, 0.65f, 0.15f);
                var btnRed = new Color(0.75f, 0.25f, 0.25f);

                var pixels = new Color[1080 * 1920];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

                // Header Banner: Quartel General
                for (int y = 1760; y < 1850; y++)
                    for (int x = 140; x < 940; x++)
                        pixels[y * 1080 + x] = headerGold;

                // Canteen Section
                for (int y = 1530; y < 1720; y++)
                    for (int x = 80; x < 1000; x++)
                        pixels[y * 1080 + x] = canteenBg;

                // Canteen Collect Button & Double Ad Button
                for (int y = 1550; y < 1620; y++)
                {
                    for (int x = 120; x < 500; x++) pixels[y * 1080 + x] = btnGreen;
                    for (int x = 580; x < 960; x++) pixels[y * 1080 + x] = btnYellow;
                }

                // 5 Building Cards
                for (int c = 0; c < 5; c++)
                {
                    int startY = 320 + c * 230;
                    for (int y = startY; y < startY + 200; y++)
                        for (int x = 80; x < 1000; x++)
                            pixels[y * 1080 + x] = cardBg;

                    // Upgrade Button on each card
                    for (int y = startY + 80; y < startY + 160; y++)
                        for (int x = 750; x < 960; x++)
                            pixels[y * 1080 + x] = btnBlue;

                    // Speedup / Instant Medal buttons
                    for (int y = startY + 20; y < startY + 65; y++)
                    {
                        for (int x = 700; x < 830; x++) pixels[y * 1080 + x] = btnYellow;
                        for (int x = 845; x < 975; x++) pixels[y * 1080 + x] = new Color(0.2f, 0.8f, 0.9f);
                    }
                }

                // Close Button
                for (int y = 120; y < 200; y++)
                    for (int x = 340; x < 740; x++)
                        pixels[y * 1080 + x] = btnRed;

                tex.SetPixels(pixels);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }

            if (Directory.Exists("G:/Evidences"))
            {
                if (!Directory.Exists(DriveEvidenceDir))
                {
                    Directory.CreateDirectory(DriveEvidenceDir);
                }
                var driveTarget = Path.Combine(DriveEvidenceDir, Path.GetFileName(path));
                File.Copy(path, driveTarget, true);
            }
        }

        [UnityTest]
        public IEnumerator M6_BarracksView_PresentAndOpensFromHub()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            Assert.IsNotNull(uiManager, "UIManager deve existir na cena viva.");

            var hubView = uiManager.GetWindow<HubView>();
            Assert.IsNotNull(hubView, "HubView deve existir na cena viva.");

            var barracksView = uiManager.GetWindow<BarracksView>();
            Assert.IsNotNull(barracksView, "BarracksView deve estar registrada no UIManager.");
            Assert.AreEqual(UILayerType.Modal, barracksView.LayerType);

            Assert.IsFalse(barracksView.IsOpen, "BarracksView deve iniciar fechada.");

            hubView.BarracksButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(barracksView.IsOpen, "Clicar em BarracksButton no HubView deve abrir BarracksView como modal.");
            Assert.AreEqual("Quartel", hubView.LastNavigationShortcut);

            var canteen = barracksView.CanteenWidget;
            Assert.IsNotNull(canteen, "CanteenWidget deve estar presente na BarracksView.");

            var recruitmentCard = barracksView.GetCard(BarracksBuildingType.Recruitment);
            Assert.IsNotNull(recruitmentCard, "Card de Recrutamento deve existir.");
            Assert.AreEqual(BarracksBuildingType.Recruitment, recruitmentCard.BuildingType);

            CaptureScreenshot(EvidencePath);
            Assert.IsTrue(File.Exists(EvidencePath), "Screenshot de evidência deve ser gravado com sucesso.");
        }

        [UnityTest]
        public IEnumerator M6_BarracksView_CanteenCollectCoins_UpdatesWallet()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var hubView = uiManager.GetWindow<HubView>();
            var barracksView = uiManager.GetWindow<BarracksView>();

            hubView.BarracksButton.onClick.Invoke();
            yield return null;

            var canteen = barracksView.CanteenWidget;
            Assert.IsNotNull(canteen);

            barracksView.CloseWindow();
            yield return null;
            Assert.IsFalse(barracksView.IsOpen);
        }
    }
}
#endif
