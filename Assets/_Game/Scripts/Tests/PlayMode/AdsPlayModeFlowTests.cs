#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.Ads;
using Game.Core.Analytics;
using Game.Core.State;
using Game.Infrastructure.Ads;
using Game.Meta.Ads;
using Game.Meta.Barracks;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Time;
using Game.Meta.Tracks;
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
    public class AdsPlayModeFlowTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const string EvidenceDir = "docs/evidencias";
        private const string DriveEvidenceDir = "G:/Evidences/HordeRunner/NEX-518/tasks";

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

        private static void CaptureScreenshot(string filename)
        {
            var localPath = Path.Combine(EvidenceDir, filename);
            var drivePath = Path.Combine(DriveEvidenceDir, filename);

            var dir = Path.GetDirectoryName(localPath);
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

                    File.WriteAllBytes(localPath, tex.EncodeToPNG());
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
                var driveDir = Path.GetDirectoryName(drivePath);
                if (!string.IsNullOrEmpty(driveDir) && !Directory.Exists(driveDir))
                {
                    Directory.CreateDirectory(driveDir);
                }
                if (File.Exists(localPath))
                {
                    File.Copy(localPath, drivePath, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator M6_Ads_Services_AreRegisteredAndConfiguredInComposition()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();
            Assert.IsNotNull(composer, "GreyboxRunComposer deve existir na cena viva.");

            Assert.IsNotNull(composer.AdService, "AdService deve ser configurado no composer.");
            Assert.IsNotNull(composer.AdPolicy, "AdPolicy deve ser configurado no composer.");
            Assert.IsNotNull(composer.TimeProvider, "TimeProvider deve ser configurado no composer.");

            var uiManager = composer.UIManager;
            Assert.IsNotNull(uiManager, "UIManager deve existir.");

            var hubView = uiManager.GetWindow<HubView>();
            Assert.IsNotNull(hubView, "HubView deve existir.");
            Assert.IsNotNull(hubView.AdService, "HubView deve receber AdService injetado.");
            Assert.IsNotNull(hubView.AdPolicy, "HubView deve receber AdPolicy injetada.");

            var resultView = uiManager.GetWindow<ResultView>();
            Assert.IsNotNull(resultView, "ResultView deve existir.");
            Assert.IsNotNull(resultView.Presenter, "ResultView deve possuir presenter.");
            Assert.IsNotNull(resultView.Presenter.AdService, "ResultRewardPresenter deve receber AdService.");
            Assert.IsNotNull(resultView.Presenter.AdPolicy, "ResultRewardPresenter deve receber AdPolicy.");

            var barracksView = uiManager.GetWindow<BarracksView>();
            Assert.IsNotNull(barracksView, "BarracksView deve existir.");
            Assert.IsNotNull(barracksView.AdService, "BarracksView deve receber AdService.");
            Assert.IsNotNull(barracksView.AdPolicy, "BarracksView deve receber AdPolicy.");

            CaptureScreenshot("NEX-1500-01-hub-ad-boost.png");
        }

        [UnityTest]
        public IEnumerator M6_Ads_ResultView_DoubleReward_FlowAndScreenshots()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();
            var uiManager = composer.UIManager;
            var resultView = uiManager.GetWindow<ResultView>();
            var mockAds = new MockAdService();
            var policy = new AdPolicy(new MonotonicTimeProvider(), new Entitlements());

            var wallet = new Wallet(composer.EventBus);
            wallet.Add(CurrencyKind.Coins, 100, "test");
            var sink = new RewardSink(wallet);
            var presenter = new ResultRewardPresenter(
                new RewardCalculator(),
                wallet,
                sink,
                composer.StateMachine,
                new MockAnalyticsService(),
                resultView,
                mockAds,
                policy);

            resultView.Initialize(presenter);
            uiManager.OpenWindow<ResultView>();
            yield return null;

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 50,
                MultiplierReached = 1,
                Stars = 3
            };
            var config = new RunConfig { LevelId = "stage_01" };
            presenter.PresentResult(runResult, config);
            yield return null;

            Assert.IsTrue(resultView.DoubleRewardButton.interactable, "Botao de dobrar moedas deve estar interactable.");

            resultView.DoubleRewardButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(mockAds.HasShown(AdPlacement.DoubleReward), "ShowAd deve ter sido invocado para DoubleReward.");
            Assert.AreEqual(100, presenter.TotalCoins, "Moedas devem ter sido dobradas de 50 para 100.");

            CaptureScreenshot("NEX-1500-02-result-double-reward.png");
        }

        [UnityTest]
        public IEnumerator M6_Ads_BarracksView_AdPlacements_Present()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();
            var uiManager = composer.UIManager;
            var hubView = uiManager.GetWindow<HubView>();
            var barracksView = uiManager.GetWindow<BarracksView>();

            hubView.BarracksButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(barracksView.IsOpen, "BarracksView deve abrir como modal.");

            var canteen = barracksView.CanteenWidget;
            Assert.IsNotNull(canteen);
            Assert.IsNotNull(canteen.AdService, "CanteenWidget deve receber AdService.");

            var recruitmentCard = barracksView.GetCard(BarracksBuildingType.Recruitment);
            Assert.IsNotNull(recruitmentCard);
            Assert.IsNotNull(recruitmentCard.AdService, "BarracksBuildingCardWidget deve receber AdService.");

            CaptureScreenshot("NEX-1500-04-barracks-ad-options.png");

            barracksView.CloseWindow();
            yield return null;
        }

        [UnityTest]
        public IEnumerator M6_Ads_InterLevel_InterstitialPacing_ExecutesOnTransitionToHub()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();
            var mockAds = new MockAdService();
            var time = new MonotonicTimeProvider();
            var entitlements = new Entitlements();
            var policy = new AdPolicy(time, entitlements);

            composer.SetAdServices(mockAds, policy, time, entitlements);
            composer.SetPlayerHighestStage(5);

            policy.State.StagesCompletedSinceLastInterstitial = 3;
            policy.State.StagesCompletedInCurrentSession = 3;
            policy.State.LastAdShownUtcTicks = 0;
            policy.State.LastRewardedOrPurchaseUtcTicks = 0;

            composer.StateMachine.TryTransition(GameState.Run);
            yield return null;

            composer.StateMachine.TryTransition(GameState.Result);
            yield return null;

            composer.StateMachine.TryTransition(GameState.Hub);
            yield return null;

            Assert.IsTrue(mockAds.HasShown(AdPlacement.InterLevel), "Ao retornar para o Hub com condicoes atendidas, interstitial deve ser exibido.");
            Assert.AreEqual(0, policy.State.StagesCompletedSinceLastInterstitial, "Contador de fases deve resetar para 0 apos interstitial.");

            CaptureScreenshot("NEX-1500-03-interstitial-triggered.png");
        }

        [UnityTest]
        public IEnumerator M6_Ads_InterLevel_EntitlementNoAds_BlocksInterstitial()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();
            var mockAds = new MockAdService();
            var time = new MonotonicTimeProvider();
            var entitlements = new Entitlements();
            entitlements.GrantEntitlement("no_ads");
            var policy = new AdPolicy(time, entitlements);

            composer.SetAdServices(mockAds, policy, time, entitlements);
            composer.SetPlayerHighestStage(5);

            policy.State.StagesCompletedSinceLastInterstitial = 5;
            policy.State.StagesCompletedInCurrentSession = 5;
            policy.State.LastAdShownUtcTicks = 0;
            policy.State.LastRewardedOrPurchaseUtcTicks = 0;

            composer.StateMachine.TryTransition(GameState.Run);
            yield return null;

            composer.StateMachine.TryTransition(GameState.Result);
            yield return null;

            composer.StateMachine.TryTransition(GameState.Hub);
            yield return null;

            Assert.IsFalse(mockAds.HasShown(AdPlacement.InterLevel), "Entitlement no_ads deve suprimir permanentemente interstitial entre fases.");
        }
    }
}
#endif
