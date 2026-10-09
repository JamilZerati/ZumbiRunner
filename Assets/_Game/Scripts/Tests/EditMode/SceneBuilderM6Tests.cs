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

        [Test]
        public void BuildM6GreyboxScene_InitializesWithSquadCount10AndPhysicalLayers()
        {
            BuildAndReopenM6Scene();

            var squad = Object.FindAnyObjectByType<SquadController>();
            Assert.IsNotNull(squad);
            Assert.AreEqual(10, squad.SquadCount);

            var mover = Object.FindAnyObjectByType<LaneMover>();
            Assert.AreEqual(CollisionLayers.SquadBodyLayer, mover.gameObject.layer);

            var poolObj = GameObject.Find("ProjectilePool");
            Assert.IsNotNull(poolObj);
            Assert.IsNull(poolObj.transform.parent);
            Assert.AreEqual(CollisionLayers.PlayerProjectileLayer, poolObj.layer);

            var spawnerObj = GameObject.Find("HordeSpawner");
            Assert.IsNotNull(spawnerObj);
            Assert.IsNull(spawnerObj.transform.parent);
            Assert.AreEqual(CollisionLayers.EnemyLayer, spawnerObj.layer);
        }

        [Test]
        public void BuildM6GreyboxScene_RunComposerWiresEveryEventBusConsumer()
        {
            BuildAndReopenM6Scene();

            var composer = Object.FindAnyObjectByType<Game.Composition.GreyboxRunComposer>();
            Assert.IsNotNull(composer);
            Assert.IsNull(composer.transform.parent);

            var serialized = new SerializedObject(composer);
            Assert.AreEqual(Object.FindAnyObjectByType<CombatDirector>(), serialized.FindProperty("combatDirector").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<HordeSpawner>(), serialized.FindProperty("hordeSpawner").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<StatusEffectDirector>(), serialized.FindProperty("statusDirector").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<Game.Gameplay.Abilities.GeneralAbilityController>(), serialized.FindProperty("abilityController").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<GeneralAbilityHud>(), serialized.FindProperty("abilityHud").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<GeneralAbilityAudio>(), serialized.FindProperty("abilityAudio").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<GeneralAbilityExplosionVfx>(), serialized.FindProperty("abilityVfx").objectReferenceValue);
            Assert.AreEqual(Object.FindAnyObjectByType<DebugTextOverlay>(), serialized.FindProperty("debugOverlay").objectReferenceValue);
        }

        [Test]
        public void BuildM6GreyboxScene_BuildsDebugOverlayUnderCanvas()
        {
            BuildAndReopenM6Scene();

            var overlay = Object.FindAnyObjectByType<DebugTextOverlay>();
            Assert.IsNotNull(overlay);
            Assert.IsNotNull(overlay.DebugText);
            Assert.IsNotNull(overlay.VisualRoot);
            Assert.AreEqual("Canvas", overlay.transform.parent.name);
        }

        [TestCase(SceneBuilder.GrenadeIconPath)]
        [TestCase(SceneBuilder.GrenadeExplosionSpritePath)]
        public void GrenadeSprites_AreImportedAsSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            Assert.IsNotNull(importer, $"Sprite ausente em {path}");
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.IsFalse(importer.mipmapEnabled);
        }

        [Test]
        public void BuildM6GreyboxScene_ExplosionVfxUsesGeneratedSpriteAtGeneral()
        {
            BuildAndReopenM6Scene();

            var vfx = Object.FindAnyObjectByType<GeneralAbilityExplosionVfx>();
            Assert.IsNotNull(vfx);
            Assert.IsNull(vfx.transform.parent, "VFX fica na raiz, fora da hierarquia do General.");
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<Sprite>(SceneBuilder.GrenadeExplosionSpritePath), vfx.Renderer.sprite);
            Assert.AreEqual(Object.FindAnyObjectByType<Game.Gameplay.Abilities.GeneralAbilityController>().transform, vfx.Origin);
            Assert.IsFalse(vfx.Renderer.enabled);
        }

        [Test]
        public void BuildM6GreyboxScene_ManualTriggerButtonShowsGrenadeIcon()
        {
            BuildAndReopenM6Scene();

            var hud = Object.FindAnyObjectByType<GeneralAbilityHud>();
            Assert.IsNotNull(hud);
            var image = hud.ManualTriggerButton.GetComponent<UnityEngine.UI.Image>();
            var expected = AssetDatabase.LoadAssetAtPath<Sprite>(SceneBuilder.GrenadeIconPath);

            Assert.IsNotNull(expected);
            Assert.AreEqual(expected, image.sprite);
            Assert.IsTrue(image.preserveAspect);
            Assert.AreEqual(image, hud.ManualTriggerButton.targetGraphic);
        }

        [Test]
        public void BuildM6GreyboxScene_AbilityAudioPlaysExplosionAndCameraListens()
        {
            BuildAndReopenM6Scene();

            var audio = Object.FindAnyObjectByType<GeneralAbilityAudio>();
            Assert.IsNotNull(audio);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<AudioClip>(SceneBuilder.GrenadeExplosionClipPath), audio.ExplosionClip);
            Assert.IsNotNull(audio.Source);
            Assert.IsFalse(audio.Source.playOnAwake);

            Assert.IsNotNull(Camera.main);
            Assert.IsNotNull(Camera.main.GetComponent<AudioListener>());
        }

        [Test]
        public void BuildM6GreyboxScene_HasEventSystemWithInputSystemModule()
        {
            BuildAndReopenM6Scene();

            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            Assert.AreEqual(1, eventSystems.Length);
            Assert.IsNotNull(eventSystems[0].GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>());
        }

        [Test]
        public void BuildM6GreyboxScene_CreatesAimIndicatorAndLinksToGeneralAbilityHud()
        {
            BuildAndReopenM6Scene();

            var hud = Object.FindAnyObjectByType<GeneralAbilityHud>();
            Assert.IsNotNull(hud, "GeneralAbilityHud must exist in M6 Greybox scene.");
            Assert.IsNotNull(hud.AimIndicator, "GeneralAbilityHud must reference AbilityAimIndicator.");

            var indicator = Object.FindAnyObjectByType<AbilityAimIndicator>();
            Assert.IsNotNull(indicator, "AbilityAimIndicator must exist in M6 Greybox scene.");
        }
    }
}
