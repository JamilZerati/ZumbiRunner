#if UNITY_EDITOR
using System.Collections;
using Game.Core;
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
    public class GreyboxRunCompositionPlayModeTests
    {
        private const string M6ScenePath = "Assets/_Game/Scenes/M6_Greybox.unity";
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
        }

        private static EnemyController FindAliveEnemy()
        {
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            {
                if (enemy.isActiveAndEnabled && enemy.IsAlive)
                {
                    return enemy;
                }
            }
            return null;
        }

        [UnityTest]
        public IEnumerator M6DoDisco_AbateNormalDeZumbiDaCena_AvancaCargaDaGranadaEHud()
        {
            yield return LoadM6FromDisk();

            var controller = Object.FindFirstObjectByType<GeneralAbilityController>();
            var hud = Object.FindFirstObjectByType<GeneralAbilityHud>();
            var enemy = FindAliveEnemy();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(hud);
            Assert.IsNotNull(enemy, "A M6 deve ter zumbis pré-posicionados vivos logo após o load.");

            int killsBefore = controller.CurrentKills;
            enemy.ReceiveHit(new DamageInfo(10000), null);

            Assert.IsFalse(enemy.IsAlive);
            Assert.AreEqual(killsBefore + 1, controller.CurrentKills, "Abate normal na cena real deve carregar a Granada.");
            Assert.AreEqual(controller.CurrentKills, hud.DisplayedKills, "HUD deve acompanhar a carga em runtime.");
        }

        [UnityTest]
        public IEnumerator M6DoDisco_AbateDeZumbiDeOndaGeradaEmRuntime_AvancaCargaDaGranada()
        {
            yield return LoadM6FromDisk();

            var controller = Object.FindFirstObjectByType<GeneralAbilityController>();
            var spawner = Object.FindFirstObjectByType<HordeSpawner>();
            Assert.IsNotNull(controller);
            Assert.IsNotNull(spawner);

            var wave = spawner.SpawnWave(0, 1, 300f);
            Assert.AreEqual(1, wave.Count);

            int killsBefore = controller.CurrentKills;
            wave[0].ReceiveHit(new DamageInfo(10000), null);

            Assert.AreEqual(killsBefore + 1, controller.CurrentKills, "Zumbi de onda nova deve publicar no barramento de runtime.");
        }
    }
}
#endif
