using System.IO;
using System.Linq;
using Game.Core.Stats;
using Game.Editor;
using Game.Gameplay;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SceneBuilderM5Tests
    {
        private static void BuildAndReopenM5Scene()
        {
            SceneBuilder.BuildM5GreyboxScene();
            EditorSceneManager.OpenScene(SceneBuilder.M5GreyboxScenePath, OpenSceneMode.Single);
        }

        private static Gate FindGateWithPerk(string perkId)
        {
            return Object.FindObjectsByType<Gate>(FindObjectsSortMode.None)
                .FirstOrDefault(g => g.Perk != null && g.Perk.Id == perkId);
        }

        [Test]
        public void M5GreyboxScenePath_IsDefinedInExpectedLocation()
        {
            Assert.AreEqual("Assets/_Game/Scenes/M5_Greybox.unity", SceneBuilder.M5GreyboxScenePath);
        }

        [Test]
        public void BuildM5GreyboxScene_CreatesSceneFile()
        {
            SceneBuilder.BuildM5GreyboxScene();

            Assert.IsTrue(File.Exists(SceneBuilder.M5GreyboxScenePath));
        }

        [Test]
        public void BuildM5GreyboxScene_WeaponControllerHasCatalogAndStartsWithPistol()
        {
            BuildAndReopenM5Scene();

            var weapon = Object.FindAnyObjectByType<WeaponController>();
            Assert.IsNotNull(weapon, "WeaponController must exist in M5 Greybox scene.");
            var serializedWeapon = new SerializedObject(weapon);
            Assert.IsNotNull(serializedWeapon.FindProperty("catalog")?.objectReferenceValue,
                "WeaponController must reference WeaponCatalog.asset.");
            Assert.AreEqual("pistol", serializedWeapon.FindProperty("initialWeaponId")?.stringValue);
        }

        [Test]
        public void BuildM5GreyboxScene_HasWeaponStatAndArithmeticGatePairsInDistanceOrder()
        {
            BuildAndReopenM5Scene();

            var pairs = Object.FindObjectsByType<GatePair>(FindObjectsSortMode.None)
                .OrderBy(p => p.transform.position.z)
                .ToArray();
            Assert.AreEqual(3, pairs.Length, "M5 scene should contain exactly 3 GatePair instances.");

            AssertPair(pairs[0], 25f, "weapon_shotgun", "weapon_smg");
            AssertPair(pairs[1], 60f, "damage_up_25", "fire_rate_up_1");
            AssertPair(pairs[2], 95f, "add_10", "multiply_2");
        }

        private static void AssertPair(GatePair pair, float expectedZ, string perkA, string perkB)
        {
            Assert.AreEqual(expectedZ, pair.transform.position.z, 0.001f);
            var perkIds = pair.Gates.Select(g => g.Perk != null ? g.Perk.Id : null).OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(new[] { perkA, perkB }.OrderBy(id => id).ToArray(), perkIds);
        }

        [Test]
        public void BuildM5GreyboxScene_LoadoutIsReachableFromTheColliderThatTriggersGates()
        {
            BuildAndReopenM5Scene();

            var mover = Object.FindAnyObjectByType<LaneMover>();
            Assert.IsNotNull(mover, "LaneMover (General) must exist in M5 Greybox scene.");
            var generalCollider = mover.GetComponent<Collider>();
            Assert.IsNotNull(generalCollider);

            Assert.IsNotNull(generalCollider.GetComponentInParent<IWeaponLoadout>(),
                "Gate.OnTriggerEnter resolves IWeaponLoadout with GetComponentInParent; unreachable loadout turns weapon gates into no-ops.");
        }

        [Test]
        public void BuildM5GreyboxScene_TriggeringShotgunGateWithGeneralCollider_EquipsShotgun()
        {
            BuildAndReopenM5Scene();

            var weapon = Object.FindAnyObjectByType<WeaponController>();
            var generalCollider = Object.FindAnyObjectByType<LaneMover>().GetComponent<Collider>();
            var shotgunGate = FindGateWithPerk("weapon_shotgun");
            Assert.IsNotNull(shotgunGate, "M5 scene must have a gate carrying weapon_shotgun.");

            shotgunGate.OnTriggerEnter(generalCollider);

            Assert.IsTrue(shotgunGate.ParentPair.IsConsumed);
            Assert.AreEqual("shotgun", weapon.EquippedWeaponId);
            Assert.AreEqual(3, weapon.CurrentStats.ProjectileCount);
        }

        [Test]
        public void BuildM5GreyboxScene_TriggeringDamageGate_AddsModifierSourcedByPerk()
        {
            BuildAndReopenM5Scene();

            var weapon = Object.FindAnyObjectByType<WeaponController>();
            var generalCollider = Object.FindAnyObjectByType<LaneMover>().GetComponent<Collider>();
            var damageGate = FindGateWithPerk("damage_up_25");
            Assert.IsNotNull(damageGate, "M5 scene must have a gate carrying damage_up_25.");

            damageGate.OnTriggerEnter(generalCollider);

            Assert.AreEqual(1, weapon.Stats.GetModifiers(StatId.Damage).Count);
            Assert.AreEqual("damage_up_25", weapon.Stats.GetModifiers(StatId.Damage)[0].Source);
        }
    }
}
