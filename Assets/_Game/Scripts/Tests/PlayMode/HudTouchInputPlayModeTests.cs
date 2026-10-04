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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class HudTouchInputPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
        private Scene _loadedScene;
        private Mouse _mouse;
        private bool _hadSavedMode;
        private int _savedMode;

        [SetUp]
        public void SetUp()
        {
            _hadSavedMode = PlayerPrefs.HasKey(PlayerPrefsAbilitySettings.DefaultPrefsKey);
            _savedMode = PlayerPrefs.GetInt(PlayerPrefsAbilitySettings.DefaultPrefsKey, 0);
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_mouse != null)
            {
                InputSystem.RemoveDevice(_mouse);
            }

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

        private IEnumerator TapAt(Vector2 screenPosition)
        {
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screenPosition });
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screenPosition }.WithButton(MouseButton.Left, true));
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screenPosition });
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator M6DoDisco_ToqueNoBotaoManualComCarga_DisparaGranada()
        {
            yield return LoadM6FromDisk();

            var controller = Object.FindFirstObjectByType<GeneralAbilityController>();
            var hud = Object.FindFirstObjectByType<GeneralAbilityHud>();
            var spawner = Object.FindFirstObjectByType<HordeSpawner>();
            hud.SetMode(HeroAbilityTriggerMode.Manual);
            foreach (var zombie in spawner.SpawnWave(0, controller.KillsPerCharge, 400f))
            {
                zombie.ReceiveHit(new DamageInfo(10000), null);
            }
            Assert.AreEqual(1, controller.CurrentCharges, "Precondição: uma carga pronta.");

            var buttonCenter = (Vector2)hud.ManualTriggerButton.transform.position;
            yield return TapAt(buttonCenter);

            Assert.AreEqual(0, controller.CurrentCharges, "Toque no botão manual deve disparar a Granada.");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_ToqueNoToggleDeModo_AlternaAutoManual()
        {
            yield return LoadM6FromDisk();

            var hud = Object.FindFirstObjectByType<GeneralAbilityHud>();
            hud.SetMode(HeroAbilityTriggerMode.Auto);

            yield return TapAt(hud.ModeToggleButton.transform.position);

            Assert.AreEqual(HeroAbilityTriggerMode.Manual, hud.CurrentMode, "Toque no toggle deve alternar para Manual.");
        }
    }
}
#endif
