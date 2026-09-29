using System.IO;
using Game.Editor;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SceneBuilderM4Tests
    {
        [Test]
        public void M4GreyboxScenePath_IsDefinedInExpectedLocation()
        {
            Assert.AreEqual("Assets/_Game/Scenes/M4_Greybox.unity", SceneBuilder.M4GreyboxScenePath);
        }

        [Test]
        public void BuildM4GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM4GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M4GreyboxScenePath));
        }

        [Test]
        public void BuildM4GreyboxScene_ContainsRequiredGameplayCombatAndEnemyComponents()
        {
            SceneBuilder.BuildM4GreyboxScene();

            // Reload the scene cleanly from disk to validate cold startup without editor transient memory
            EditorSceneManager.OpenScene(SceneBuilder.M4GreyboxScenePath, OpenSceneMode.Single);

            var mover = Object.FindAnyObjectByType<LaneMover>();
            Assert.IsNotNull(mover, "LaneMover must exist in M4 Greybox scene.");

            var rb = mover.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb, "General must have a Rigidbody for physics trigger interactions.");
            Assert.IsTrue(rb.isKinematic, "General Rigidbody must be kinematic.");
            Assert.IsFalse(rb.useGravity, "General Rigidbody must not use gravity.");

            var col = mover.GetComponent<Collider>();
            Assert.IsNotNull(col, "General must have a Collider component.");

            var scroller = Object.FindAnyObjectByType<TrackScroller>();
            Assert.IsNotNull(scroller, "TrackScroller must exist in M4 Greybox scene.");
            Assert.AreEqual(8.0f, scroller.ForwardSpeed, "TrackScroller forward speed should be 8.0.");

            var squad = Object.FindAnyObjectByType<SquadController>();
            Assert.IsNotNull(squad, "SquadController must exist in M4 Greybox scene.");
            Assert.AreEqual(3, squad.SquadCount, "SquadController should be initialized with 3 soldiers.");

            var visual = Object.FindAnyObjectByType<SquadVisualController>();
            Assert.IsNotNull(visual, "SquadVisualController must exist in M4 Greybox scene.");

            var weapon = Object.FindAnyObjectByType<WeaponController>();
            Assert.IsNotNull(weapon, "WeaponController must exist in M4 Greybox scene.");
            Assert.AreEqual(2.0f, weapon.FireRate, "WeaponController fire rate should be 2.");
            Assert.AreEqual(2, weapon.DamagePerShot, "WeaponController damage should be 2.");
            Assert.IsNotNull(weapon.Pool, "WeaponController must have a projectile pool initialized.");

            var director = Object.FindAnyObjectByType<CombatDirector>();
            Assert.IsNotNull(director, "CombatDirector must exist in M4 Greybox scene.");
            Assert.AreSame(squad, director.Squad, "CombatDirector must reference SquadController.");
            Assert.AreSame(scroller, director.Scroller, "CombatDirector must reference TrackScroller.");
            Assert.AreEqual(120f, director.VictoryDistance, "CombatDirector victory distance should be 120.");
            Assert.IsFalse(director.IsResolved, "CombatDirector should start unresolved.");

            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            Assert.IsNotNull(spawner, "HordeSpawner must exist in M4 Greybox scene.");
            Assert.AreSame(spawner, director.Spawner, "CombatDirector must reference HordeSpawner.");
            Assert.IsNotNull(spawner.Pool, "HordeSpawner must have an enemy pool initialized.");
            Assert.IsNotNull(spawner.Layout, "HordeSpawner must have a lane layout initialized.");

            var enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            Assert.Greater(enemies.Length, 0, "Scene must contain pre-spawned enemy waves.");
            foreach (var enemy in enemies)
            {
                Assert.IsTrue(enemy.IsActiveInPool, "Pre-spawned enemy must be active in pool.");
                Assert.IsTrue(enemy.IsAlive, "Pre-spawned enemy must be alive.");
            }

            var hud = Object.FindAnyObjectByType<SquadCountHud>();
            Assert.IsNotNull(hud, "SquadCountHud must exist in M4 Greybox scene.");

            var cam = Object.FindAnyObjectByType<FollowCamera>();
            Assert.IsNotNull(cam, "FollowCamera must exist in M4 Greybox scene.");
            Assert.AreSame(mover.transform, cam.Target, "FollowCamera must target General.");

            var gatePairs = Object.FindObjectsByType<GatePair>(FindObjectsSortMode.None);
            Assert.AreEqual(3, gatePairs.Length, "Scene should contain exactly 3 GatePair instances.");

            var gates = Object.FindObjectsByType<Gate>(FindObjectsSortMode.None);
            Assert.AreEqual(6, gates.Length, "Scene should contain exactly 6 Gate instances (2 per pair).");
        }
    }
}
