using System.IO;
using System.Linq;
using Game.Core;
using Game.Core.Status;
using Game.Editor;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SceneBuilderM6Tests
    {
        private static void BuildAndReopenM6Scene()
        {
            SceneBuilder.BuildM6GreyboxScene();
            EditorSceneManager.OpenScene(SceneBuilder.M6GreyboxScenePath, OpenSceneMode.Single);
        }

        private static Gate FindGateWithPerk(string perkId)
        {
            return Object.FindObjectsByType<Gate>(FindObjectsSortMode.None)
                .FirstOrDefault(g => g.Perk != null && g.Perk.Id == perkId);
        }

        [Test]
        public void M6GreyboxScenePath_IsDefinedInExpectedLocation()
        {
            Assert.AreEqual("Assets/_Game/Scenes/M6_Greybox.unity", SceneBuilder.M6GreyboxScenePath);
        }

        [Test]
        public void BuildM6GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM6GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M6GreyboxScenePath));
        }

        [Test]
        public void BuildM6GreyboxScene_StatusEffectDirectorHasCatalogAndInteractions()
        {
            BuildAndReopenM6Scene();

            var director = Object.FindAnyObjectByType<StatusEffectDirector>();
            Assert.IsNotNull(director, "StatusEffectDirector must exist in M6 Greybox scene.");
            var serialized = new SerializedObject(director);
            Assert.IsNotNull(serialized.FindProperty("catalog")?.objectReferenceValue,
                "StatusEffectDirector must reference StatusCatalog.asset.");
            Assert.IsNotNull(serialized.FindProperty("interactions")?.objectReferenceValue,
                "StatusEffectDirector must reference EffectInteractionTable.asset.");
        }

        [Test]
        public void BuildM6GreyboxScene_HasAmmoStatAndMultiplicationGatePairsInDistanceOrder()
        {
            BuildAndReopenM6Scene();

            var pairs = Object.FindObjectsByType<GatePair>(FindObjectsSortMode.None)
                .OrderBy(p => p.transform.position.z)
                .ToArray();
            Assert.AreEqual(3, pairs.Length, "M6 scene should contain exactly 3 GatePair instances.");

            AssertPair(pairs[0], 25f, "ammo_cryo", "ammo_fire");
            AssertPair(pairs[1], 60f, "damage_up_25", "ammo_toxic");
            AssertPair(pairs[2], 95f, "ammo_shock", "multiply_2");
        }

        private static void AssertPair(GatePair pair, float expectedZ, string perkA, string perkB)
        {
            Assert.AreEqual(expectedZ, pair.transform.position.z, 0.001f);
            var perkIds = pair.Gates.Select(g => g.Perk != null ? g.Perk.Id : null).OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(new[] { perkA, perkB }.OrderBy(id => id).ToArray(), perkIds);
        }

        [Test]
        public void BuildM6GreyboxScene_PreSpawnedEnemiesHaveStatusDirectorAndHealth40()
        {
            BuildAndReopenM6Scene();

            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None)
                .Where(e => e.gameObject.activeInHierarchy)
                .ToArray();
            Assert.Greater(enemies.Length, 0, "M6 scene must have active pre-spawned enemies.");

            foreach (var enemy in enemies)
            {
                var serialized = new SerializedObject(enemy);
                Assert.IsNotNull(serialized.FindProperty("statusDirector")?.objectReferenceValue,
                    "Pre-spawned enemy must reference StatusEffectDirector.");
                Assert.AreEqual(40, enemy.MaxHealth, "Pre-spawned enemy must have 40 MaxHealth.");
                Assert.IsNotNull(enemy.GetComponent<EnemyStatusTint>(),
                    "Pre-spawned enemy must have EnemyStatusTint component.");
            }
        }

        [Test]
        public void BuildM6GreyboxScene_TriggeringAmmoCryoGate_AddsFreezeAndSlowToLoadout()
        {
            BuildAndReopenM6Scene();

            var weapon = Object.FindAnyObjectByType<WeaponController>();
            var generalCollider = Object.FindAnyObjectByType<LaneMover>().GetComponent<Collider>();
            var cryoGate = FindGateWithPerk("ammo_cryo");
            Assert.IsNotNull(cryoGate, "M6 scene must have a gate carrying ammo_cryo.");

            cryoGate.OnTriggerEnter(generalCollider);

            Assert.IsTrue(cryoGate.ParentPair.IsConsumed);
            var snapshot = weapon.OnHitStatuses.Snapshot();
            Assert.AreEqual(2, snapshot.Length);
            Assert.IsTrue(snapshot.Any(s => s.Kind == StatusKind.Freeze && object.Equals(s.Source, "ammo_cryo")));
            Assert.IsTrue(snapshot.Any(s => s.Kind == StatusKind.Slow && object.Equals(s.Source, "ammo_cryo")));
        }

        [Test]
        public void EnemyStatusTint_ColorFor_ResolvesPrioritiesCorrectly()
        {
            Assert.AreEqual(Color.magenta, EnemyStatusTint.ColorFor(null, Color.magenta));

            var catalog = InMemoryStatusCatalog.CreateDefault();
            var health = new FakeHealth();
            var controller = new StatusEffectController(health, null, catalog);

            Assert.AreEqual(Color.magenta, EnemyStatusTint.ColorFor(controller, Color.magenta));

            // Freeze alone: bluish white
            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this));
            Assert.AreEqual(new Color(0.8f, 0.9f, 1f), EnemyStatusTint.ColorFor(controller, Color.magenta));

            // Slow > Freeze: light blue
            controller.Apply(new StatusApplication(StatusKind.Slow, 1, this));
            Assert.AreEqual(new Color(0.4f, 0.7f, 1f), EnemyStatusTint.ColorFor(controller, Color.magenta));

            // Poison > Slow: green
            controller.Apply(new StatusApplication(StatusKind.Poison, 1, this));
            Assert.AreEqual(Color.green, EnemyStatusTint.ColorFor(controller, Color.magenta));

            // Burn > Poison: orange
            controller.Apply(new StatusApplication(StatusKind.Burn, 1, this));
            Assert.AreEqual(new Color(1f, 0.5f, 0f), EnemyStatusTint.ColorFor(controller, Color.magenta));

            // Frozen > Burn: cyan
            controller.Apply(new StatusApplication(StatusKind.Freeze, 1, this)); // Threshold is 2, so freeze 1 + 1 triggers Frozen
            Assert.IsTrue(controller.Has(StatusKind.Frozen));
            Assert.AreEqual(Color.cyan, EnemyStatusTint.ColorFor(controller, Color.magenta));
        }

        private sealed class FakeHealth : Game.Core.IDamageable
        {
            public int CurrentHealth { get; set; } = 100;
            public int MaxHealth { get; set; } = 100;
            public bool IsAlive => CurrentHealth > 0;
            public void TakeDamage(DamageInfo damage) => CurrentHealth = System.Math.Max(0, CurrentHealth - damage.Amount);
        }
    }
}
