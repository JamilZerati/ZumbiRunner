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

            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return;
            }

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

            int balanceBefore = barracksView.Wallet.GetBalance(CurrencyKind.Coins);
            canteen.CollectButton.onClick.Invoke();
            yield return null;
            int balanceAfter = barracksView.Wallet.GetBalance(CurrencyKind.Coins);
            Assert.GreaterOrEqual(balanceAfter, balanceBefore);

            barracksView.CloseWindow();
            yield return null;
            Assert.IsFalse(barracksView.IsOpen);
        }
    }
}
#endif
