using System.Collections.Generic;
using Game.Core.Stats;
using Game.Data;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    // A cena reaberta em EditMode (SceneBuilderM5Tests) usa o controller sem Awake/Start/Initialize,
    // e EnsurePoolInitialized chama Initialize sem catálogo: o catálogo serializado não pode se perder.
    public class WeaponControllerCatalogWiringTests
    {
        private const float Tolerance = 0.0001f;

        private List<Object> toDestroy;
        private WeaponController weapon;
        private ObjectPool<Projectile> pool;

        [SetUp]
        public void SetUp()
        {
            toDestroy = new List<Object>();
            var weaponObject = Track(new GameObject("TestWeaponCatalogWiring"));
            weapon = weaponObject.AddComponent<WeaponController>();
            pool = new ObjectPool<Projectile>(() => Track(new GameObject("PooledProjectile")).AddComponent<Projectile>());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = toDestroy.Count - 1; i >= 0; i--)
            {
                if (toDestroy[i] != null)
                {
                    Object.DestroyImmediate(toDestroy[i]);
                }
            }
            toDestroy.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            toDestroy.Add(obj);
            return obj;
        }

        private WeaponCatalog CreateCatalogAsset(params WeaponProfile[] profiles)
        {
            var definitions = new List<WeaponDefinition>();
            foreach (var profile in profiles)
            {
                var definition = Track(ScriptableObject.CreateInstance<WeaponDefinition>());
                definition.SetData(profile.Id, profile.Id, profile.FireRate, profile.Damage, profile.ProjectileSpeed,
                                   profile.Range, profile.ProjectilesPerShot, profile.SpreadWidth);
                definitions.Add(definition);
            }

            var catalog = Track(ScriptableObject.CreateInstance<WeaponCatalog>());
            catalog.SetWeapons(definitions);
            return catalog;
        }

        private void AssignSerializedCatalog(WeaponCatalog catalog)
        {
            var serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("catalog").objectReferenceValue = catalog;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void FreshController_WithoutAnyInitialization_HasPistolBasesAndNoEquippedWeapon()
        {
            Assert.IsNotNull(weapon.Stats);
            Assert.AreEqual(string.Empty, weapon.EquippedWeaponId);
            Assert.AreEqual(2f, weapon.CurrentStats.FireRate, Tolerance);
            Assert.AreEqual(2, weapon.CurrentStats.Damage);
            Assert.AreEqual(15f, weapon.CurrentStats.ProjectileSpeed, Tolerance);
            Assert.AreEqual(40f, weapon.CurrentStats.Range, Tolerance);
            Assert.AreEqual(1, weapon.CurrentStats.ProjectileCount);
            Assert.AreEqual(0f, weapon.CurrentStats.SpreadWidth, Tolerance);
        }

        [Test]
        public void SerializedCatalog_WithoutInitialize_TryEquipUsesIt()
        {
            AssignSerializedCatalog(CreateCatalogAsset(WeaponTestCatalog.Pistol, WeaponTestCatalog.Shotgun));

            Assert.IsTrue(weapon.TryEquip("shotgun"));

            Assert.AreEqual("shotgun", weapon.EquippedWeaponId);
            Assert.AreEqual(3, weapon.CurrentStats.ProjectileCount);
        }

        [Test]
        public void InitializeWithoutCatalog_KeepsSerializedCatalog()
        {
            AssignSerializedCatalog(CreateCatalogAsset(WeaponTestCatalog.Pistol, WeaponTestCatalog.Shotgun));

            weapon.Initialize(pool, null);

            Assert.IsTrue(weapon.TryEquip("shotgun"));
            Assert.AreEqual("shotgun", weapon.EquippedWeaponId);
        }

        [Test]
        public void InjectedCatalog_TakesPrecedenceOverSerializedCatalog()
        {
            var serializedShotgun = new WeaponProfile("shotgun", 1f, 99, 5f, 5f, 1, 0f);
            AssignSerializedCatalog(CreateCatalogAsset(serializedShotgun));

            weapon.Initialize(pool, null, WeaponTestCatalog.CreateDefault());

            Assert.IsTrue(weapon.TryEquip("shotgun"));
            Assert.AreEqual(8f, weapon.Stats.GetBase(StatId.Damage), Tolerance);
        }
    }
}
