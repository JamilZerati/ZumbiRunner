using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Gates;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class LivingGatesPlayModeTests
    {
        private const int DefaultEnemyHealth = 100;
        private const int ProjectileDamage = 15;
        private const float ProjectileSpeed = 20f;
        private const float GateZ = 5f;
        private const float EnemyZ = 10f;
        private const float TestTimeout = 2.0f;

        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.Destroy(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator ShootToUpgrade_CollidesAndEvolvesGate_RecyclesProjectile_LeavesDistantEnemyUnharmed()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 2, hitsPerStep: 1, cap: 15);
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant);
            var view = AttachGateView(gate);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);

            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);
            Assert.AreEqual(-5, gate.RuntimeState.CurrentValue);

            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);

            float elapsed = 0f;
            while (elapsed < TestTimeout && projectile.IsActiveInPool)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();

            Assert.IsFalse(projectile.IsActiveInPool, "Projetil DEVE ser reciclado ao atingir portao ShootToUpgrade.");
            Assert.AreEqual(-3, gate.RuntimeState.CurrentValue, "Portao DEVE evoluir em +2 apos o primeiro tiro.");
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth, "Zumbi atras do portao ShootToUpgrade NAO DEVE receber dano.");
        }

        [UnityTest]
        public IEnumerator StaticGate_ProjectilePassesThrough_DoesNotEvolveGate_HitsAndDamagesEnemy()
        {
            var variant = new StaticGateVariant();
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);
            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);

            yield return WaitForEnemyHitOrTimeout(enemy);

            Assert.AreEqual(DefaultEnemyHealth - ProjectileDamage, enemy.CurrentHealth, "Projetil DEVE atravessar portao Static e acertar o zumbi.");
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil DEVE ser reciclado apenas ao colidir com o zumbi.");
            Assert.AreEqual(0, gate.RuntimeState.CurrentHits, "Portao Static NAO DEVE registrar hits de projetil.");
        }

        [UnityTest]
        public IEnumerator CursedGate_ProjectilePassesThrough_DoesNotEvolveGate_HitsAndDamagesEnemy()
        {
            var variant = new CursedGateVariant("perk_bonus", new StageModifierSpec("cursed", 1.2f, 1.1f), 100f);
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);
            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);

            yield return WaitForEnemyHitOrTimeout(enemy);

            Assert.AreEqual(DefaultEnemyHealth - ProjectileDamage, enemy.CurrentHealth, "Projetil DEVE atravessar portao Cursed e acertar o zumbi.");
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil DEVE ser reciclado apenas no zumbi.");
            Assert.AreEqual(0, gate.RuntimeState.CurrentHits, "Portao Cursed NAO DEVE registrar hits de projetil.");
        }

        [UnityTest]
        public IEnumerator GuardedGate_ProjectilePassesThrough_DoesNotEvolveGate_HitsAndDamagesEnemy()
        {
            var variant = new GuardedGateVariant("wave_1", "runner", 5, 8f);
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);
            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);

            yield return WaitForEnemyHitOrTimeout(enemy);

            Assert.AreEqual(DefaultEnemyHealth - ProjectileDamage, enemy.CurrentHealth, "Projetil DEVE atravessar portao Guarded e acertar o zumbi.");
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil DEVE ser reciclado apenas no zumbi.");
            Assert.AreEqual(0, gate.RuntimeState.CurrentHits, "Portao Guarded NAO DEVE registrar hits de projetil.");
        }

        [UnityTest]
        public IEnumerator ConsumedGate_ProjectilePassesThrough_HitsAndDamagesEnemy()
        {
            var pairGo = new GameObject("GatePair");
            spawnedObjects.Add(pairGo);
            var pair = pairGo.AddComponent<GatePair>();

            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 2, hitsPerStep: 1, cap: 15);
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant, pair);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);

            pair.TryTrigger(0, new TestSquad(10));
            Assert.IsTrue(pair.IsConsumed, "GatePair DEVE estar consumido.");
            Assert.IsFalse(gate.IsAlive, "Portao consumido NAO DEVE ser IDamageable ativo.");

            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);

            yield return WaitForEnemyHitOrTimeout(enemy);

            Assert.AreEqual(DefaultEnemyHealth - ProjectileDamage, enemy.CurrentHealth, "Projetil DEVE atravessar portao consumido e atingir o zumbi.");
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil DEVE ser reciclado apenas no zumbi.");
            Assert.AreEqual(-5, gate.RuntimeState.CurrentValue, "Valor do portao consumido NAO DEVE ser alterado por tiros.");
        }

        [UnityTest]
        public IEnumerator ShootToUpgrade_SequentialHits_EvolveFromNegativeToPositive_UpdateViewColor()
        {
            var variant = new ShootToUpgradeGateVariant(initialValue: -3, step: 2, hitsPerStep: 1, cap: 3);
            var gate = CreateTestGate(new Vector3(0f, 0f, GateZ), variant);
            var view = AttachGateView(gate);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), DefaultEnemyHealth);

            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);
            Assert.AreEqual(-3, gate.RuntimeState.CurrentValue);

            yield return ShootSingleProjectileAndWaitHit(gate);
            Assert.AreEqual(-1, gate.RuntimeState.CurrentValue);
            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth);

            yield return ShootSingleProjectileAndWaitHit(gate);
            Assert.AreEqual(1, gate.RuntimeState.CurrentValue);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth);

            yield return ShootSingleProjectileAndWaitHit(gate);
            Assert.AreEqual(3, gate.RuntimeState.CurrentValue);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth);

            yield return ShootSingleProjectileAndWaitHit(gate);
            Assert.AreEqual(3, gate.RuntimeState.CurrentValue, "Valor DEVE respeitar o teto cap=3.");
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth);
        }

        private IEnumerator WaitForEnemyHitOrTimeout(EnemyController enemy)
        {
            float elapsed = 0f;
            while (elapsed < TestTimeout && enemy.CurrentHealth == DefaultEnemyHealth)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator ShootSingleProjectileAndWaitHit(Gate gate)
        {
            var projectile = SpawnProjectile(Vector3.zero, ProjectileDamage, ProjectileSpeed);
            float elapsed = 0f;
            while (elapsed < TestTimeout && projectile.IsActiveInPool)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil disparado DEVE ser reciclado ao colidir com portao ShootToUpgrade.");
        }

        private Gate CreateTestGate(Vector3 position, IGateVariant variant, GatePair pair = null, int lane = 0)
        {
            var gateGo = new GameObject("TestGate");
            spawnedObjects.Add(gateGo);
            gateGo.transform.position = position;
            gateGo.layer = CollisionLayers.PickupLayer;

            var collider = gateGo.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 3f, 1f);

            var rb = gateGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var gate = gateGo.AddComponent<Gate>();
            gate.SetVariant(variant);
            gate.Initialize(lane, (PerkDefinition)null, pair);
            return gate;
        }

        private GateView AttachGateView(Gate gate)
        {
            var view = gate.gameObject.AddComponent<GateView>();

            var panelGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawnedObjects.Add(panelGo);
            panelGo.name = "Panel";
            panelGo.transform.SetParent(gate.transform, false);
            panelGo.transform.localScale = new Vector3(1.6f, 2.3f, 0.1f);
            Object.Destroy(panelGo.GetComponent<Collider>());
            var renderer = panelGo.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit"));

            view.PanelRenderer = renderer;
            view.Initialize(gate);
            return view;
        }

        private EnemyController CreateTestEnemy(Vector3 position, int initialHealth)
        {
            var enemyGo = new GameObject("TestEnemy");
            spawnedObjects.Add(enemyGo);
            enemyGo.transform.position = position;
            enemyGo.layer = CollisionLayers.EnemyLayer;

            var collider = enemyGo.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
            collider.height = 2f;

            var rb = enemyGo.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            enemyGo.AddComponent<HealthComponent>();
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, initialHealth, 0f, null);
            return enemy;
        }

        private Projectile SpawnProjectile(Vector3 position, int damage, float speed)
        {
            var projectileGo = new GameObject("TestProjectile");
            spawnedObjects.Add(projectileGo);
            projectileGo.transform.position = position;
            projectileGo.layer = CollisionLayers.PlayerProjectileLayer;

            var collider = projectileGo.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            var projectile = projectileGo.AddComponent<Projectile>();
            projectile.Initialize(damage, speed, maxDistance: 30f, onRecycle: null);
            return projectile;
        }

        private sealed class TestSquad : ISquad
        {
            public int SquadCount { get; private set; }
            public TestSquad(int initialCount) => SquadCount = initialCount;
            public bool Add(int amount) { SquadCount += Mathf.Max(0, amount); return true; }
            public bool Remove(int amount) { SquadCount = Mathf.Max(0, SquadCount - amount); return true; }
            public bool Multiply(int factor) { SquadCount = Mathf.Max(0, SquadCount * factor); return true; }
            public bool Divide(int divisor) { SquadCount = divisor > 0 ? Mathf.Max(0, SquadCount / divisor) : SquadCount; return true; }
            public bool SetCount(int newCount) { SquadCount = Mathf.Max(0, newCount); return true; }
        }
    }
}
