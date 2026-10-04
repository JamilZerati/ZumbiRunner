#if UNITY_EDITOR
using System.Collections;
using Game.Core;
using Game.Core.Abilities;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class GeneralAbilityRuntimePlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private const float FarAwayZ = 400f;
        private Scene _loadedScene;
        private bool _hadSavedMode;
        private int _savedMode;

        [SetUp]
        public void SetUp()
        {
            _hadSavedMode = PlayerPrefs.HasKey(PlayerPrefsAbilitySettings.DefaultPrefsKey);
            _savedMode = PlayerPrefs.GetInt(PlayerPrefsAbilitySettings.DefaultPrefsKey, 0);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_hadSavedMode)
            {
                PlayerPrefs.SetInt(PlayerPrefsAbilitySettings.DefaultPrefsKey, _savedMode);
            }
            else
            {
                PlayerPrefs.DeleteKey(PlayerPrefsAbilitySettings.DefaultPrefsKey);
            }

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

        private static void KillFarAwayZombies(HordeSpawner spawner, int count)
        {
            foreach (var zombie in spawner.SpawnWave(0, count, FarAwayZ))
            {
                zombie.ReceiveHit(new DamageInfo(10000), null);
            }
        }

        [UnityTest]
        public IEnumerator M6DoDisco_ModoManual_25AbatesHabilitamBotaoEToqueDisparaGranadaComSom()
        {
            yield return LoadM6FromDisk();

            var controller = Object.FindFirstObjectByType<GeneralAbilityController>();
            var hud = Object.FindFirstObjectByType<GeneralAbilityHud>();
            var audio = Object.FindFirstObjectByType<GeneralAbilityAudio>();
            var spawner = Object.FindFirstObjectByType<HordeSpawner>();
            hud.SetMode(HeroAbilityTriggerMode.Manual);

            KillFarAwayZombies(spawner, controller.KillsPerCharge);

            Assert.AreEqual(1, controller.CurrentCharges);
            Assert.IsTrue(hud.ManualTriggerButton.interactable, "Botão manual deve habilitar com carga.");
            Assert.AreEqual(0, audio.PlayedCount, "Modo Manual não dispara sozinho.");

            hud.ManualTriggerButton.onClick.Invoke();

            Assert.AreEqual(0, controller.CurrentCharges);
            Assert.AreEqual(1, audio.PlayedCount, "Disparo manual deve tocar a explosão.");
            Assert.IsFalse(hud.ManualTriggerButton.interactable);
        }

        [UnityTest]
        public IEnumerator M6DoDisco_ModoAuto_25oAbateComAlvoNaLaneDisparaGranadaComSom()
        {
            yield return LoadM6FromDisk();

            var controller = Object.FindFirstObjectByType<GeneralAbilityController>();
            var hud = Object.FindFirstObjectByType<GeneralAbilityHud>();
            var audio = Object.FindFirstObjectByType<GeneralAbilityAudio>();
            var spawner = Object.FindFirstObjectByType<HordeSpawner>();
            hud.SetMode(HeroAbilityTriggerMode.Auto);

            spawner.SpawnWave(0, 1, controller.transform.position.z + 3f);
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(controller.HasTargetsInLane(), "Precondição: zumbi vivo perto do General.");

            KillFarAwayZombies(spawner, controller.KillsPerCharge);

            Assert.AreEqual(0, controller.CurrentCharges, "Auto deve consumir a carga no 25º abate.");
            Assert.AreEqual(1, audio.PlayedCount, "Disparo automático deve tocar a explosão.");
        }
    }
}
#endif
