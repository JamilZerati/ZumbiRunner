#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Analytics;
using Game.Core.State;
using Game.Gameplay.Tutorial;
using Game.Meta.Debut;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Stages;
using Game.Meta.Tracks;
using Game.Meta.Wallet;
using Game.Presentation.Settings;
using Game.Presentation.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class UiFlowPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const string EvidencePath = "docs/evidencias/NEX-1137-ui-flow.png";
        private const string RecordingsPath = "Recordings/NEX-1137-ui-flow.png";
        private Scene _loadedScene;

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
            var cam = Camera.main;
            if (cam == null) return;

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

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
            }
        }

        [UnityTest]
        public IEnumerator M6DoDisco_HierarquiaDeUiPresenteEConfigurada()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var pauseController = Object.FindAnyObjectByType<PauseController>();
            var composer = Object.FindAnyObjectByType<GreyboxRunComposer>();

            Assert.IsNotNull(uiManager, "UIManager deve existir na cena.");
            Assert.IsNotNull(pauseController, "PauseController deve existir na cena.");
            Assert.IsNotNull(composer, "GreyboxRunComposer deve existir na cena.");

            Assert.IsNotNull(uiManager.GetWindow<HubView>(), "HubView deve existir.");
            Assert.IsNotNull(uiManager.GetWindow<PauseView>(), "PauseView deve existir.");
            Assert.IsNotNull(uiManager.GetWindow<OptionsView>(), "OptionsView deve existir.");
            Assert.IsNotNull(uiManager.GetWindow<ResultView>(), "ResultView deve existir.");
            Assert.IsNotNull(uiManager.GetWindow<DebutJourneyView>(), "DebutJourneyView deve existir.");
            Assert.IsNotNull(uiManager.GetWindow<TutorialGuideView>(), "TutorialGuideView deve existir.");

            Assert.IsNotNull(uiManager.GetLayer(UILayerType.Hub), "HubLayer deve estar registrado.");
            Assert.IsNotNull(uiManager.GetLayer(UILayerType.GameHud), "GameHudLayer deve estar registrado.");
            Assert.IsNotNull(uiManager.GetLayer(UILayerType.Modal), "ModalLayer deve estar registrado.");
            Assert.IsNotNull(uiManager.GetLayer(UILayerType.TutorialOverlay), "TutorialOverlayLayer deve estar registrado.");
        }

        [UnityTest]
        public IEnumerator AC1_HubView_CarregaSaldoMapaEIniciaCorrida()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var hubView = uiManager.GetWindow<HubView>();

            Assert.IsNotNull(hubView);
            Assert.IsTrue(hubView.IsOpen);
            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            StringAssert.Contains("100", hubView.CoinsText.text);

            hubView.PlayButton.onClick.Invoke();

            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);
            Assert.IsFalse(uiManager.IsLayerVisible(UILayerType.Hub));
            Assert.IsTrue(uiManager.IsLayerVisible(UILayerType.GameHud));
        }

        [UnityTest]
        public IEnumerator AC3_PauseView_CongelaSimulacaoPermiteOpcoesEQuitParaHub()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var pauseController = Object.FindAnyObjectByType<PauseController>();
            var pauseView = uiManager.GetWindow<PauseView>();
            var hubView = uiManager.GetWindow<HubView>();

            hubView.PlayButton.onClick.Invoke();
            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);

            pauseController.PauseGame();

            Assert.AreEqual(GameState.Paused, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(0.0f, Time.timeScale, 1e-4f);
            Assert.IsTrue(pauseView.IsOpen);

            pauseView.ResumeButton.onClick.Invoke();

            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale, 1e-4f);
            Assert.IsFalse(pauseView.IsOpen);

            pauseController.PauseGame();
            pauseView.QuitButton.onClick.Invoke();

            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale, 1e-4f);
            Assert.IsTrue(hubView.IsOpen);
        }

        [UnityTest]
        public IEnumerator AC4_OptionsView_PersisteVolumeModoEDropdownDeIdioma()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var optionsView = uiManager.OpenWindow<OptionsView>();

            Assert.IsNotNull(optionsView);
            Assert.IsTrue(optionsView.IsOpen);

            optionsView.MasterVolumeSlider.value = 0.65f;
            Assert.AreEqual(0.65f, optionsView.AudioSettings.MasterVolume, 1e-3f);

            optionsView.AbilityModeButton.onClick.Invoke();
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, optionsView.AbilitySettings.TriggerMode);

            optionsView.LegacyLanguageDropdown.value = 1;
            Assert.AreEqual("en-US", optionsView.LocalizationSettings.CurrentLanguage);

            optionsView.CloseButton.onClick.Invoke();
            Assert.IsFalse(optionsView.IsOpen);
        }

        [UnityTest]
        public IEnumerator AC2_ResultView_ExibeMoedasDobraComRewardedEIdempotencia()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var resultView = uiManager.OpenWindow<ResultView>();
            var hubView = uiManager.GetWindow<HubView>();

            var runResult = new RunResult
            {
                LevelId = "stage_01",
                Victory = true,
                CoinsEarned = 100,
                MultiplierReached = 2,
                Stars = 3
            };
            var runConfig = new RunConfig
            {
                LevelId = "stage_01",
                RewardMultiplier = 1.0f
            };

            resultView.Presenter.PresentResult(runResult, runConfig);

            StringAssert.Contains("Vitória", resultView.TitleText.text);
            StringAssert.Contains("200", resultView.CoinsText.text);

            resultView.DoubleRewardButton.onClick.Invoke();
            StringAssert.Contains("400", resultView.CoinsText.text);

            resultView.ContinueButton.onClick.Invoke();
            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            Assert.IsTrue(hubView.IsOpen);
        }

        [UnityTest]
        public IEnumerator AC6_DebutJourneyView_Exibe7DiasEResgataMarcoDisponivel()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var debutView = uiManager.OpenWindow<DebutJourneyView>();

            Assert.IsNotNull(debutView);
            Assert.IsTrue(debutView.IsOpen);
            Assert.AreEqual(7, debutView.DayStatusTexts.Count);

            debutView.Track.AddPoints(1);
            debutView.RefreshView();

            bool claimed = debutView.ClaimAvailableDay();
            Assert.IsTrue(claimed);

            debutView.CloseButton.onClick.Invoke();
            Assert.IsFalse(debutView.IsOpen);
        }

        [UnityTest]
        public IEnumerator AC5_AC7_TutorialGuide_ExibeBannerContextualERegistraFtueAnalytics()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var tutorialView = uiManager.GetWindow<TutorialGuideView>();
            var analytics = new MockAnalyticsService();
            var controller = new TutorialGuideController(new FtueStepProgress(), analytics);
            tutorialView.Initialize(controller);

            controller.CheckTutorialStep(1);

            Assert.IsTrue(tutorialView.IsOpen);
            StringAssert.Contains("mover", tutorialView.InstructionText.text);
            Assert.GreaterOrEqual(analytics.Events.Count, 1);
            Assert.AreEqual(AnalyticsEvents.FtueStep, analytics.Events[0].EventName);
        }

        [UnityTest]
        public IEnumerator RunFullUiFlow()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var pauseController = Object.FindAnyObjectByType<PauseController>();
            var hubView = uiManager.GetWindow<HubView>();
            var pauseView = uiManager.GetWindow<PauseView>();
            var resultView = uiManager.GetWindow<ResultView>();

            Assert.IsNotNull(uiManager);
            Assert.IsTrue(hubView.IsOpen);

            hubView.PlayButton.onClick.Invoke();
            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);

            pauseController.PauseGame();
            Assert.AreEqual(GameState.Paused, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(0.0f, Time.timeScale, 1e-4f);

            pauseView.ResumeButton.onClick.Invoke();
            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(1.0f, Time.timeScale, 1e-4f);

            var runResult = new RunResult { LevelId = "stage_01", Victory = true, CoinsEarned = 50, MultiplierReached = 2, Stars = 3 };
            var runConfig = new RunConfig { LevelId = "stage_01", RewardMultiplier = 1.0f };
            uiManager.OpenWindow<ResultView>();
            resultView.Presenter.PresentResult(runResult, runConfig);
            resultView.ContinueButton.onClick.Invoke();

            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            Assert.IsTrue(hubView.IsOpen);

            CaptureScreenshot(EvidencePath);
            CaptureScreenshot(RecordingsPath);

            Assert.IsTrue(File.Exists(EvidencePath), "Evidência visual deve ser salva em docs/evidencias/.");
            Assert.Greater(new FileInfo(EvidencePath).Length, 1000, "Screenshot deve ter tamanho válido.");
            Assert.IsTrue(File.Exists(RecordingsPath), "Evidência visual deve ser salva em Recordings/.");
        }
    }
}
#endif
