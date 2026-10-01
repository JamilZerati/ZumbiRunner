using System.Collections.Generic;
using Game.Core;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Status;
using Game.Data;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class OnHitStatusPipelineTests
    {
        private List<Object> toDestroy;

        [SetUp]
        public void SetUp()
        {
            toDestroy = new List<Object>();
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

        private WeaponController CreateWeapon(string initialWeaponId = "pistol")
        {
            var go = Track(new GameObject("Weapon"));
            var weapon = go.AddComponent<WeaponController>();
            weapon.Initialize(new ObjectPool<Projectile>(() => Track(new GameObject("P")).AddComponent<Projectile>()),
                              null, WeaponTestCatalog.CreateDefault());
            weapon.TryEquip(initialWeaponId);
            return weapon;
        }

        private sealed class FakeSquad : ISquad
        {
            public int SquadCount => 1;
            public bool Add(int amount) => true;
            public bool Remove(int amount) => true;
            public bool Multiply(int factor) => true;
            public bool Divide(int divisor) => true;
            public bool SetCount(int targetCount) => true;
        }

        [Test]
        public void AmmoCryoPerk_AddsFreezeAndSlowToWeaponOnHitStatuses()
        {
            var weapon = CreateWeapon("pistol");
            var pair = Track(new GameObject("Pair")).AddComponent<GatePair>();
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData("ammo_cryo", "Ammo Cryo", new List<IPerkEffect>
            {
                new ApplyStatusOnHitEffect { Status = StatusKind.Freeze, Stacks = 1 },
                new ApplyStatusOnHitEffect { Status = StatusKind.Slow, Stacks = 1 }
            });

            var gateGo = Track(new GameObject("Gate"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, perk, pair);

            bool triggered = pair.TryTrigger(0, new FakeSquad(), null, weapon);
            Assert.IsTrue(triggered);

            Assert.IsNotNull(weapon.OnHitStatuses);
            Assert.AreEqual(2, weapon.OnHitStatuses.Items.Count);
            Assert.AreEqual(StatusKind.Freeze, weapon.OnHitStatuses.Items[0].Kind);
            Assert.AreEqual(StatusKind.Slow, weapon.OnHitStatuses.Items[1].Kind);
        }

        [Test]
        public void WeaponLoadout_TryEquip_PreservesOnHitStatuses()
        {
            var weapon = CreateWeapon("pistol");
            weapon.OnHitStatuses.Add(new StatusApplication(StatusKind.Burn, 1, "test_source"));

            weapon.TryEquip("shotgun");
            Assert.AreEqual("shotgun", weapon.EquippedWeaponId);
            Assert.AreEqual(1, weapon.OnHitStatuses.Items.Count);
            Assert.AreEqual(StatusKind.Burn, weapon.OnHitStatuses.Items[0].Kind);
        }

        [Test]
        public void OnHitStatusSet_RemoveAllFromSource_ClearsOnlyMatchingEntries()
        {
            var set = new OnHitStatusSet();
            set.Add(new StatusApplication(StatusKind.Burn, 1, "ammo_fire"));
            set.Add(new StatusApplication(StatusKind.Slow, 1, "ammo_cryo"));
            set.Add(new StatusApplication(StatusKind.Freeze, 1, "ammo_cryo"));

            int removed = set.RemoveAllFromSource("ammo_cryo");
            Assert.AreEqual(2, removed);
            Assert.AreEqual(1, set.Items.Count);
            Assert.AreEqual("ammo_fire", set.Items[0].Source);
        }

        [Test]
        public void Projectile_Initialize_SnapshotPreservesPayloadAndResetOnRecycle()
        {
            var go = Track(new GameObject("Proj"));
            var proj = go.AddComponent<Projectile>();

            var payload = new[] { new StatusApplication(StatusKind.Freeze, 1, "ammo_cryo") };
            proj.Initialize(10, 15f, 40f, null, 0, onHit: payload);

            Assert.IsNotNull(proj.OnHit);
            Assert.AreEqual(1, proj.OnHit.Count);
            Assert.AreEqual(StatusKind.Freeze, proj.OnHit[0].Kind);

            // Reusing projectile from pool without payload resets OnHit to empty
            proj.Initialize(10, 15f, 40f, null, 0);
            Assert.AreEqual(0, proj.OnHit.Count, "Projectile reused without payload must have empty OnHit.");
        }

        [Test]
        public void Projectile_HandleTrigger_AppliesOnHitStatusToEnemyController()
        {
            var projGo = Track(new GameObject("Proj"));
            var proj = projGo.AddComponent<Projectile>();
            var onHit = new[] { new StatusApplication(StatusKind.Freeze, 1, "ammo_cryo") };
            proj.Initialize(10, 15f, 40f, null, 0, onHit: onHit);

            var enemyGo = Track(new GameObject("Enemy"));
            var health = enemyGo.AddComponent<HealthComponent>();
            health.Initialize(40);
            var enemy = enemyGo.AddComponent<EnemyController>();

            proj.HandleTrigger(enemyGo);

            Assert.IsTrue(enemy.Status.Has(StatusKind.Freeze), "Freeze status should be applied to enemy.");
            Assert.AreEqual(1, enemy.Status.GetStacks(StatusKind.Freeze));
        }
    }
}
