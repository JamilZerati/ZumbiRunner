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
using Game.Presentation;
using Game.Presentation.Settings;
using Game.Presentation.UI;
using AudioSettings = Game.Presentation.Settings.AudioSettings;
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

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ResetPlayerPrefs();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1.0f;
            ResetPlayerPrefs();

            if (_loadedScene.IsValid() && _loadedScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(_loadedScene);
            }
        }

        private static void ResetPlayerPrefs()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsAbilitySettings.DefaultPrefsKey);
            PlayerPrefs.DeleteKey(AudioSettings.MasterKey);
            PlayerPrefs.DeleteKey(AudioSettings.SfxKey);
            PlayerPrefs.DeleteKey(AudioSettings.BgmKey);
            PlayerPrefs.DeleteKey(LocalizationSettings.LanguageKey);
            PlayerPrefs.Save();
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
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                // Em ambiente -nographics (sem GPU), nao sobrescrever screenshot valido capturado com dispositivo grafico
                return;
            }

            var cam = Camera.main;
            Assert.IsNotNull(cam, "Camera.main não deve ser nula para captura de evidência visual.");

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

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
            var optionsView = uiManager.GetWindow<OptionsView>();
            var hubView = uiManager.GetWindow<HubView>();

            hubView.PlayButton.onClick.Invoke();
            Assert.AreEqual(GameState.Run, uiManager.StateMachine.CurrentState);

            pauseController.PauseGame();

            Assert.AreEqual(GameState.Paused, uiManager.StateMachine.CurrentState);
            Assert.AreEqual(0.0f, Time.timeScale, 1e-4f);
            Assert.IsTrue(pauseView.IsOpen);

            pauseView.OptionsButton.onClick.Invoke();
            Assert.IsTrue(optionsView.IsOpen, "OptionsView deve abrir a partir do menu de pausa.");
            optionsView.CloseButton.onClick.Invoke();
            Assert.IsFalse(optionsView.IsOpen, "OptionsView deve fechar pelo CloseButton.");
            Assert.AreEqual(0.0f, Time.timeScale, 1e-4f, "Time.timeScale deve permanecer congelado em 0 após fechar OptionsView.");
            Assert.IsTrue(pauseView.IsOpen, "PauseView deve permanecer aberto após fechar OptionsView.");

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
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, optionsView.AbilitySettings.TriggerMode, "Modo inicial deve ser Auto.");

            optionsView.MasterVolumeSlider.value = 0.65f;
            Assert.AreEqual(0.65f, optionsView.AudioSettings.MasterVolume, 1e-3f);

            optionsView.AbilityModeButton.onClick.Invoke();
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, optionsView.AbilitySettings.TriggerMode);

            optionsView.LegacyLanguageDropdown.value = 1;
            Assert.AreEqual("en-US", optionsView.LocalizationSettings.CurrentLanguage);

            optionsView.CloseButton.onClick.Invoke();
            Assert.IsFalse(optionsView.IsOpen);

            Assert.AreEqual(0.65f, PlayerPrefs.GetFloat(AudioSettings.MasterKey), 1e-3f);
            Assert.AreEqual((int)HeroAbilityTriggerMode.Manual, PlayerPrefs.GetInt(PlayerPrefsAbilitySettings.DefaultPrefsKey));
            Assert.AreEqual("en-US", PlayerPrefs.GetString(LocalizationSettings.LanguageKey));

            var reloadedAudio = new AudioSettings();
            var reloadedAbility = new PlayerPrefsAbilitySettings();
            var reloadedLang = new LocalizationSettings();

            Assert.AreEqual(0.65f, reloadedAudio.MasterVolume, 1e-3f);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, reloadedAbility.TriggerMode);
            Assert.AreEqual("en-US", reloadedLang.CurrentLanguage);
        }

        [UnityTest]
        public IEnumerator AC2_ResultView_ExibeMoedasDobraComRewardedEIdempotencia()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var resultView = uiManager.OpenWindow<ResultView>();
            var hubView = uiManager.GetWindow<HubView>();

            uiManager.StateMachine.TryTransition(GameState.Run);
            uiManager.StateMachine.TryTransition(GameState.Result);
            Assert.AreEqual(GameState.Result, uiManager.StateMachine.CurrentState);

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

            resultView.DoubleRewardButton.onClick.Invoke();
            StringAssert.Contains("400", resultView.CoinsText.text);
            Assert.IsFalse(resultView.DoubleRewardButton.interactable, "Botão de dobrar deve estar desabilitado após resgate.");

            resultView.ContinueButton.onClick.Invoke();
            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            Assert.IsTrue(hubView.IsOpen);
            StringAssert.Contains("500", hubView.CoinsText.text, "Saldo da carteira deve ser atualizado para 100 + 400 = 500 no Hub.");

            resultView.ContinueButton.onClick.Invoke();
            StringAssert.Contains("500", hubView.CoinsText.text, "Segundo clique não deve duplicar concessão de moedas.");
            Assert.IsFalse(resultView.ContinueButton.interactable, "Botão Continuar deve permanecer desabilitado.");
        }

        [UnityTest]
        public IEnumerator AC6_DebutJourneyView_Exibe7DiasEResgataMarcoDisponivel()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindAnyObjectByType<UIManager>();
            var hubView = uiManager.GetWindow<HubView>();
            var debutView = uiManager.OpenWindow<DebutJourneyView>();

            Assert.IsNotNull(debutView);
            Assert.IsTrue(debutView.IsOpen);
            Assert.AreEqual(7, debutView.DayStatusTexts.Count);
            StringAssert.Contains("100", hubView.CoinsText.text, "Saldo inicial deve ser 100.");

            debutView.Track.AddPoints(1);
            debutView.RefreshView();
            Assert.IsTrue(debutView.ClaimButton.interactable, "ClaimButton deve estar habilitado para dia disponível.");

            debutView.ClaimButton.onClick.Invoke();
            StringAssert.Contains("Resgatado", debutView.DayStatusTexts[0].text);
            StringAssert.Contains("200", hubView.CoinsText.text, "Saldo da carteira deve ser incrementado em 100 moedas após o resgate.");
            Assert.IsFalse(debutView.ClaimButton.interactable, "ClaimButton deve ser desabilitado após resgate.");

            debutView.ClaimButton.onClick.Invoke();
            StringAssert.Contains("200", hubView.CoinsText.text, "Segundo clique não deve alterar o saldo.");
            Assert.IsFalse(debutView.ClaimButton.interactable, "ClaimButton permanece desabilitado.");

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
            Assert.IsTrue(tutorialView.BannerRoot.activeSelf, "BannerRoot deve estar ativo ao exibir o passo.");
            StringAssert.Contains("mover", tutorialView.InstructionText.text);
            Assert.GreaterOrEqual(analytics.Events.Count, 1);
            Assert.AreEqual(AnalyticsEvents.FtueStep, analytics.Events[0].EventName);
            Assert.AreEqual(FtueStepProgress.SwipeStep, analytics.Events[0].Parameters["step_name"]);
            Assert.AreEqual(1, analytics.Events[0].Parameters["stage_index"]);
            Assert.AreEqual("started", analytics.Events[0].Parameters["status"]);

            controller.CompleteTutorialStep(FtueStepProgress.SwipeStep, 1);
            Assert.AreEqual(2, analytics.Events.Count);
            Assert.AreEqual(AnalyticsEvents.FtueStep, analytics.Events[1].EventName);
            Assert.AreEqual(FtueStepProgress.SwipeStep, analytics.Events[1].Parameters["step_name"]);
            Assert.AreEqual(1, analytics.Events[1].Parameters["stage_index"]);
            Assert.AreEqual("completed", analytics.Events[1].Parameters["status"]);
            Assert.IsFalse(tutorialView.BannerRoot.activeSelf, "BannerRoot deve ser ocultado ao concluir o passo.");
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
            uiManager.StateMachine.TryTransition(GameState.Result);
            uiManager.OpenWindow<ResultView>();
            resultView.Presenter.PresentResult(runResult, runConfig);
            resultView.ContinueButton.onClick.Invoke();

            Assert.AreEqual(GameState.Hub, uiManager.StateMachine.CurrentState);
            Assert.IsTrue(hubView.IsOpen);

            CaptureScreenshot(EvidencePath);
            CaptureScreenshot(RecordingsPath);

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                var googleDriveDir = "G:/Meu Drive/Evidences/ZumbiRunner/NEX-1137/tasks";
                if (Directory.Exists(googleDriveDir))
                {
                    var googleDriveFile = Path.Combine(googleDriveDir, "NEX-1137-ui-flow.png");
                    CaptureScreenshot(googleDriveFile);
                    Assert.IsTrue(File.Exists(googleDriveFile), "Evidência visual deve ser salva no Google Drive.");
                }

                Assert.IsTrue(File.Exists(EvidencePath), "Evidência visual deve ser salva em docs/evidencias/.");
                Assert.Greater(new FileInfo(EvidencePath).Length, 1000, "Screenshot deve ter tamanho válido.");
                Assert.IsTrue(File.Exists(RecordingsPath), "Evidência visual deve ser salva em Recordings/.");
            }
        }
    }
}
#endif
