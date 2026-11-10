using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Core.Stats;
using Game.Data;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.Gameplay
{
    [TestFixture]
    public class WeaponControllerBarracksTests
    {
        private List<Object> _toDestroy;
        private WeaponController _weapon;
        private ObjectPool<Projectile> _pool;
        private EventBus _eventBus;

        [SetUp]
        public void SetUp()
        {
            _toDestroy = new List<Object>();
            var weaponObject = Track(new GameObject("TestWeaponControllerBarracks"));
            _weapon = weaponObject.AddComponent<WeaponController>();
            _eventBus = new EventBus();
            _pool = new ObjectPool<Projectile>(() => Track(new GameObject("PooledProjectile")).AddComponent<Projectile>());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _toDestroy.Count - 1; i >= 0; i--)
            {
                if (_toDestroy[i] != null)
                {
                    Object.DestroyImmediate(_toDestroy[i]);
                }
            }
            _toDestroy.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            _toDestroy.Add(obj);
            return obj;
        }

        private WeaponCatalog CreateCatalog(params WeaponProfile[] profiles)
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

        [Test]
        public void CT_14_Initialize_WithRunConfig_AppliesBarracksArsenalDamageBonus()
        {
            var catalog = CreateCatalog(new WeaponProfile("rifle", 2f, 20, 15f, 40f, 1, 0f));
            var runConfig = new RunConfig { BaseDamageMultiplier = 1.05f };

            _weapon.Initialize(_pool, null, catalog, _eventBus, runConfig);
            _weapon.TryEquip("rifle");

            Assert.AreEqual(21, _weapon.DamagePerShot);
            var modifiers = _weapon.Stats.GetModifiers(StatId.Damage);
            Assert.IsTrue(modifiers.Count > 0 && Equals(modifiers[0].Source, WeaponController.BarracksArsenalSource) && modifiers[0].Kind == ModifierKind.PercentAdd);
        }

        [Test]
        public void CT_14_Initialize_CalledTwice_DoesNotStackBarracksArsenalBonus()
        {
            var catalog = CreateCatalog(new WeaponProfile("rifle", 2f, 20, 15f, 40f, 1, 0f));
            var runConfig = new RunConfig { BaseDamageMultiplier = 1.05f };

            _weapon.Initialize(_pool, null, catalog, _eventBus, runConfig);
            _weapon.TryEquip("rifle");
            Assert.AreEqual(21, _weapon.DamagePerShot);

            _weapon.Initialize(_pool, null, catalog, _eventBus, runConfig);
            Assert.AreEqual(21, _weapon.DamagePerShot, "Modificador BarracksArsenal não deve acumular entre corridas.");
        }

        [Test]
        public void CT_14_TryEquip_PreservesBarracksArsenalDamageBonus()
        {
            var catalog = CreateCatalog(
                new WeaponProfile("rifle", 2f, 20, 15f, 40f, 1, 0f),
                new WeaponProfile("shotgun", 1f, 40, 10f, 20f, 3, 2f)
            );
            var runConfig = new RunConfig { BaseDamageMultiplier = 1.05f };

            _weapon.Initialize(_pool, null, catalog, _eventBus, runConfig);
            _weapon.TryEquip("rifle");
            Assert.AreEqual(21, _weapon.DamagePerShot);

            bool equipped = _weapon.TryEquip("shotgun");
            Assert.IsTrue(equipped);
            Assert.AreEqual(42, _weapon.DamagePerShot, "Troca de arma via TryEquip deve preservar o bônus do Arsenal.");
        }

        [Test]
        public void ApplyBarracksDamageBonus_WhenMultiplierOneOrLess_CleansUpModifier()
        {
            var catalog = CreateCatalog(new WeaponProfile("rifle", 2f, 20, 15f, 40f, 1, 0f));
            var runConfig = new RunConfig { BaseDamageMultiplier = 1.10f };

            _weapon.Initialize(_pool, null, catalog, _eventBus, runConfig);
            _weapon.TryEquip("rifle");
            Assert.AreEqual(22, _weapon.DamagePerShot);

            _weapon.ApplyBarracksDamageBonus(1.0f);
            Assert.AreEqual(20, _weapon.DamagePerShot);
        }
    }
}
