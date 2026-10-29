#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Game.Composition;
using Game.Core;
using Game.Core.Boss;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.State;
using Game.Data.Mutations;
using Game.Gameplay;
using Game.Meta.Progression;
using Game.Meta.Rewards;
using Game.Meta.Run;
using Game.Meta.Save;
using Game.Meta.Wallet;
using Game.Presentation;
using Game.Presentation.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class MutationFlowPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const string EvidenceHubPath = "docs/evidencias/NEX-655-01-hub-mutations.png";
        private const string EvidenceRunPath = "docs/evidencias/NEX-655-02-run-mutations.png";
        private const string GoogleDriveEvidenceDir = "G:/Evidences/HordeRunner/NEX-655";
        private const string ZumbiRunnerEvidenceDir = "G:/Evidences/ZumbiRunner/NEX-655";
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
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
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

                byte[] bytes = tex.EncodeToPNG();
                File.WriteAllBytes(path, bytes);

                if (Directory.Exists(GoogleDriveEvidenceDir))
                {
                    string gdPath = Path.Combine(GoogleDriveEvidenceDir, Path.GetFileName(path));
                    File.WriteAllBytes(gdPath, bytes);
                }

                if (Directory.Exists(ZumbiRunnerEvidenceDir))
                {
                    string zrPath = Path.Combine(ZumbiRunnerEvidenceDir, Path.GetFileName(path));
                    File.WriteAllBytes(zrPath, bytes);
                }

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
            }
        }

        [UnityTest]
        public IEnumerator M6DoDisco_MutationWidget_RegistradoEHierarquiaPresente()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindFirstObjectByType<UIManager>();
            Assert.IsNotNull(uiManager, "UIManager deve existir na cena M6.");

            var hubView = uiManager.GetWindow<HubView>();
            Assert.IsNotNull(hubView, "HubView deve estar registrado no UIManager.");

            var widget = hubView.MutationSelectionWidget;
            Assert.IsNotNull(widget, "MutationSelectionWidget deve estar associado ao HubView.");
            Assert.IsNotNull(widget.ItemsContainer, "ItemsContainer do MutationSelectionWidget deve estar configurado.");
            Assert.IsNotNull(widget.TotalBonusText, "TotalBonusText do MutationSelectionWidget deve estar configurado.");
            Assert.IsNotNull(widget.LockedOverlay, "LockedOverlay deve estar configurado.");
            Assert.GreaterOrEqual(widget.Items.Count, 5, "MutationSelectionWidget deve conter os 5 itens canônicos de mutação.");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_EstadoBloqueadoAntesDoBoss1_NaoPermiteSelecao()
        {
            yield return LoadM6FromDisk();

            var hubView = Object.FindFirstObjectByType<HubView>();
            Assert.IsNotNull(hubView);

            var widget = hubView.MutationSelectionWidget;
            Assert.IsNotNull(widget);

            Assert.IsFalse(widget.AreMutationsUnlocked, "Mutações devem iniciar bloqueadas antes da derrota do Boss 1.");
            Assert.IsTrue(widget.LockedOverlay.activeSelf, "Overlay de bloqueio deve estar visível.");

            bool toggled = widget.ToggleMutation("armored");
            Assert.IsFalse(toggled, "ToggleMutation deve ser rejeitado quando o recurso está bloqueado.");
            Assert.AreEqual(0, hubView.SelectedMutations.Count, "Nenhuma mutação pode estar selecionada no estado bloqueado.");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_DesbloqueioEFluxoDeSelecao_RespeitaTetoDeTres()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindFirstObjectByType<GreyboxRunComposer>();
            var hubView = Object.FindFirstObjectByType<HubView>();
            Assert.IsNotNull(composer);
            Assert.IsNotNull(hubView);

            var widget = hubView.MutationSelectionWidget;
            Assert.IsNotNull(widget);

            var metaState = new MetaState();
            metaState.ClaimedGrantIds.Add("unlock_boss_level_05_boss_weapon:shotgun");
            var unlockedProgression = new ProgressionService(metaState: metaState);

            widget.Initialize(composer.MutationCatalog, unlockedProgression);

            Assert.IsTrue(widget.AreMutationsUnlocked, "Mutações devem estar desbloqueadas após derrota do Boss 1.");
            Assert.IsFalse(widget.LockedOverlay.activeSelf, "Overlay de bloqueio deve estar oculto.");

            bool t1 = widget.ToggleMutation("armored");
            bool t2 = widget.ToggleMutation("frenzy");
            bool t3 = widget.ToggleMutation("fog");

            Assert.IsTrue(t1);
            Assert.IsTrue(t2);
            Assert.IsTrue(t3);
            Assert.AreEqual(3, hubView.SelectedMutations.Count, "Exatamente 3 mutações devem estar selecionadas.");
            Assert.AreEqual(1.25f, widget.TotalBonusPercentage, 1e-4f, "Bônus total deve ser a soma de +50% + 50% + 25% = 125%.");

            bool t4 = widget.ToggleMutation("last_soldier");
            Assert.IsFalse(t4, "Tentativa de selecionar 4ª mutação deve ser rejeitada pelo teto rígido de 3 escolhas.");
            Assert.AreEqual(3, hubView.SelectedMutations.Count);
        }

        [UnityTest]
        public IEnumerator M6DoDisco_InicioDeCorridaComMutacoes_AplicaModificadoresEmGameplay()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindFirstObjectByType<GreyboxRunComposer>();
            var hubView = Object.FindFirstObjectByType<HubView>();
            var combatDirector = Object.FindFirstObjectByType<CombatDirector>();
            var hordeSpawner = Object.FindFirstObjectByType<HordeSpawner>();

            Assert.IsNotNull(composer);
            Assert.IsNotNull(hubView);
            Assert.IsNotNull(combatDirector);
            Assert.IsNotNull(hordeSpawner);

            var metaState = new MetaState();
            metaState.ClaimedGrantIds.Add("unlock_boss_level_05_boss_weapon:shotgun");
            var unlockedProgression = new ProgressionService(metaState: metaState);
            hubView.MutationSelectionWidget.Initialize(composer.MutationCatalog, unlockedProgression);

            hubView.MutationSelectionWidget.ToggleMutation("armored");
            hubView.MutationSelectionWidget.ToggleMutation("frenzy");
            hubView.MutationSelectionWidget.ToggleMutation("fog");

            hubView.SelectStage("level_01");
            bool runStarted = hubView.StartSelectedRun();
            Assert.IsTrue(runStarted, "StartSelectedRun deve iniciar com sucesso.");
            yield return null;

            Assert.AreEqual(GameState.Run, composer.StateMachine.CurrentState);

            var modifiers = combatDirector.StageModifiers;
            Assert.IsNotNull(modifiers, "StageModifiers deve estar ativo no CombatDirector.");
            Assert.AreEqual(1.5f, modifiers.CurrentEnemyHealthMultiplier, 1e-4f, "Blindados deve aplicar 1.5x no HP dos inimigos.");
            Assert.AreEqual(1.3f, modifiers.CurrentEnemySpeedMultiplier, 1e-4f, "Frenesi deve aplicar 1.3x na velocidade dos zumbis.");
            Assert.AreEqual(0.6f, combatDirector.EffectiveSquadRangeMultiplier, 1e-4f, "Neblina deve reduzir alcance em 40% (0.6x).");

            Assert.AreEqual((int)System.Math.Round(hordeSpawner.DefaultEnemyHealth * 1.5f), hordeSpawner.EffectiveEnemyHealth, "HP efetivo de spawn deve refletir o multiplicador de Blindados (20 * 1.5 = 30).");
            Assert.AreEqual(2.6f, hordeSpawner.EffectiveEnemySpeed, 1e-4f, "Velocidade efetiva de spawn deve refletir Frenesi (2.0 * 1.3 = 2.6).");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_FinalizacaoDeCorrida_CalculaBonusDeMutacaoNoResultView()
        {
            yield return LoadM6FromDisk();

            var uiManager = Object.FindFirstObjectByType<UIManager>();
            var resultView = uiManager.GetWindow<ResultView>();
            Assert.IsNotNull(resultView, "ResultView deve estar presente e registrado.");

            var result = new RunResult
            {
                Victory = true,
                LevelId = "level_01",
                MultiplierReached = 2,
                CoinsEarned = 100
            };

            var config = new RunConfig
            {
                LevelId = "level_01",
                MutationIds = new List<string> { "armored", "frenzy" }
            };

            resultView.Presenter.PresentResult(result, config);

            Assert.IsNotNull(resultView.Presenter.CurrentBundle, "Bundle de recompensas deve ser calculado.");
            Assert.Greater(resultView.Presenter.TotalCoins, 0, "Total de moedas deve ser concedido com bônus acumulado.");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_CapturaEvidenciasVisuais_SalvaHubERun()
        {
            yield return LoadM6FromDisk();

            var composer = Object.FindFirstObjectByType<GreyboxRunComposer>();
            var hubView = Object.FindFirstObjectByType<HubView>();
            Assert.IsNotNull(composer);
            Assert.IsNotNull(hubView);

            var metaState = new MetaState();
            metaState.ClaimedGrantIds.Add("unlock_boss_level_05_boss_weapon:shotgun");
            var unlockedProgression = new ProgressionService(metaState: metaState);
            hubView.MutationSelectionWidget.Initialize(composer.MutationCatalog, unlockedProgression);
            hubView.MutationSelectionWidget.ToggleMutation("armored");
            hubView.MutationSelectionWidget.ToggleMutation("frenzy");

            CaptureScreenshot(EvidenceHubPath);

            hubView.SelectStage("level_01");
            hubView.StartSelectedRun();
            yield return null;

            CaptureScreenshot(EvidenceRunPath);

            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.IsTrue(File.Exists(EvidenceHubPath), "Evidência visual do Hub com mutações deve ser gerada.");
                Assert.IsTrue(File.Exists(EvidenceRunPath), "Evidência visual da Corrida com mutações deve ser gerada.");
            }
        }
    }
}
#endif
