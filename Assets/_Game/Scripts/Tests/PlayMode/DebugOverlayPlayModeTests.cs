#if UNITY_EDITOR
using System.Collections;
using System.IO;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class DebugOverlayPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const string EvidencePath = "docs/evidencias/NEX-781-m6-debug-overlay.png";
        private Scene _loadedScene;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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
            yield return null;
        }

        [UnityTest]
        public IEnumerator M6DoDisco_ContemDebugOverlayAtivoEAtualizado()
        {
            yield return LoadM6FromDisk();

            var overlay = Object.FindFirstObjectByType<DebugTextOverlay>();
            Assert.IsNotNull(overlay, "DebugTextOverlay deve existir na cena M6 montada pelo SceneBuilder.");
            Assert.IsTrue(overlay.IsVisible, "Overlay deve iniciar visível em Development/Editor.");
            Assert.IsTrue(overlay.HasDebugText, "DebugText deve estar atribuído.");

            overlay.Refresh();

            string text = overlay.CurrentText;
            Assert.IsNotEmpty(text);
            StringAssert.Contains("[DEBUG OVERLAY]", text);
            StringAssert.Contains("Tropa:", text);
            StringAssert.Contains("Arma:", text);
            StringAssert.Contains("Granada:", text);

            overlay.Toggle();
            Assert.IsFalse(overlay.IsVisible);
            if (overlay.VisualRoot != null)
            {
                Assert.IsFalse(overlay.VisualRoot.activeSelf);
            }

            overlay.Toggle();
            Assert.IsTrue(overlay.IsVisible);
            if (overlay.VisualRoot != null)
            {
                Assert.IsTrue(overlay.VisualRoot.activeSelf);
            }
        }

        [UnityTest]
        public IEnumerator M6DoDisco_CapturaEvidenciaVisualEm1080x1920()
        {
            yield return LoadM6FromDisk();

            var overlay = Object.FindFirstObjectByType<DebugTextOverlay>();
            Assert.IsNotNull(overlay);
            overlay.Refresh();

            var canvas = overlay.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas);

            var cam = Camera.main;
            Assert.IsNotNull(cam);

            var originalRenderMode = canvas.renderMode;
            var originalCamera = canvas.worldCamera;

            var rt = new RenderTexture(1080, 1920, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                try
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = 1f;

                    cam.targetTexture = rt;
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
                    tex.Apply();

                    var dir = Path.GetDirectoryName(EvidencePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    byte[] png = tex.EncodeToPNG();
                    File.WriteAllBytes(EvidencePath, png);
                    Object.DestroyImmediate(tex);
                }
                finally
                {
                    cam.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    canvas.renderMode = originalRenderMode;
                    canvas.worldCamera = originalCamera;
                    rt.Release();
                    Object.DestroyImmediate(rt);
                }
            }
            else
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            Assert.IsTrue(File.Exists(EvidencePath), "Arquivo de evidência deve ser gerado.");
            var fileInfo = new FileInfo(EvidencePath);
            Assert.Greater(fileInfo.Length, 10000, "Screenshot deve conter dados visuais legíveis.");
        }
    }
}
#endif
