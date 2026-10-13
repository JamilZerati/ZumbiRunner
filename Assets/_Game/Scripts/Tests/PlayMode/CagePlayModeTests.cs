using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class CagePlayModeTests
    {
        private const int DefaultEnemyHealth = 100;
        private const float ProjectileSpeed = 20f;
        private const float CageZ = 5f;
        private const float EnemyZ = 10f;
        private const float TestTimeout = 2.0f;

        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                if (_spawnedObjects[i] != null)
                {
                    Object.Destroy(_spawnedObjects[i]);
                }
            }
            _spawnedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator Projectile_PhysicalCollisionWithCage_DamagesCageAndRecyclesProjectile()
        {
            var cage = CreateTestCage(new Vector3(0f, 0f, CageZ), hp: 100, survivors: 3);
            var projectile = SpawnProjectile(Vector3.zero, damage: 35, speed: ProjectileSpeed);

            yield return WaitForProjectileRecycleOrTimeout(projectile);

            Assert.IsFalse(projectile.IsActiveInPool, "Projetil deve ser reciclado apos atingir a jaula.");
            Assert.AreEqual(65, cage.CurrentHealth, "HP da jaula deve diminuir pelo dano do projetil.");
            Assert.IsTrue(cage.IsAlive, "Jaula com HP > 0 deve permanecer viva.");
            Assert.IsFalse(cage.IsOpen, "Jaula nao deve abrir antes de zerar o HP.");
        }

        [UnityTest]
        public IEnumerator Projectile_SequentialHits_OpensCage_IntegratesSurvivorsToSquad_PublishesEvent()
        {
            var squad = new TestSquad(initialCount: 5);
            var eventBus = new TestEventBus();
            var cage = CreateTestCage(new Vector3(0f, 0f, CageZ), hp: 50, survivors: 4, squad: squad, eventBus: eventBus);

            yield return ShootSingleProjectileAndWaitHit(damage: 25);
            Assert.AreEqual(25, cage.CurrentHealth);
            Assert.AreEqual(5, squad.SquadCount);
            Assert.IsTrue(cage.IsAlive);

            yield return ShootSingleProjectileAndWaitHit(damage: 25);
            Assert.AreEqual(0, cage.CurrentHealth);
            Assert.IsTrue(cage.IsOpen);
            Assert.IsFalse(cage.IsAlive);
            Assert.IsFalse(cage.CageCollider.enabled, "Colisor da jaula deve ser desativado apos abertura.");
            Assert.AreEqual(9, squad.SquadCount, "Sobreviventes da jaula devem ser adicionados a tropa.");

            Assert.AreEqual(1, eventBus.RescuedEvents.Count);
            Assert.AreEqual(4, eventBus.RescuedEvents[0].Count);
        }

        [UnityTest]
        public IEnumerator Cage_BlocksProjectiles_ProtectingEnemyBehind_UntilOpened()
        {
            var cage = CreateTestCage(new Vector3(0f, 0f, CageZ), hp: 20, survivors: 2);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), initialHealth: DefaultEnemyHealth);

            yield return ShootSingleProjectileAndWaitHit(damage: 10);
            Assert.AreEqual(10, cage.CurrentHealth);
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth, "Inimigo atras da jaula viva nao deve receber dano.");

            yield return ShootSingleProjectileAndWaitHit(damage: 10);
            Assert.AreEqual(0, cage.CurrentHealth);
            Assert.IsTrue(cage.IsOpen);
            Assert.AreEqual(DefaultEnemyHealth, enemy.CurrentHealth, "Inimigo atras da jaula deve continuar intacto no momento da abertura.");

            var projectile = SpawnProjectile(Vector3.zero, damage: 30, speed: ProjectileSpeed);
            yield return WaitForEnemyHitOrTimeout(enemy, expectedHealth: DefaultEnemyHealth - 30);

            Assert.AreEqual(70, enemy.CurrentHealth, "Apos a jaula abrir, projetil subsequente deve atravessar e atingir o inimigo.");
            Assert.IsFalse(projectile.IsActiveInPool);
        }

        [UnityTest]
        public IEnumerator LostCage_AfterGeneralPasses_DisablesCollider_DoesNotBlockProjectiles()
        {
            var cage = CreateTestCage(new Vector3(0f, 0f, CageZ), hp: 100, survivors: 3);
            var enemy = CreateTestEnemy(new Vector3(0f, 0f, EnemyZ), initialHealth: DefaultEnemyHealth);

            bool escaped = cage.CheckEscape(generalZ: 6f);
            Assert.IsTrue(escaped);
            Assert.IsTrue(cage.IsLost);
            Assert.IsFalse(cage.IsAlive);
            Assert.IsFalse(cage.CageCollider.enabled);

            var projectile = SpawnProjectile(Vector3.zero, damage: 25, speed: ProjectileSpeed);
            yield return WaitForEnemyHitOrTimeout(enemy, expectedHealth: DefaultEnemyHealth - 25);

            Assert.AreEqual(75, enemy.CurrentHealth, "Projetil deve atravessar jaula perdida e acertar o inimigo.");
            Assert.IsFalse(projectile.IsActiveInPool);
        }

        [UnityTest]
        public IEnumerator CageView_AttachedToCage_UpdatesVisualsInRealtime()
        {
            var cage = CreateTestCage(new Vector3(0f, 0f, CageZ), hp: 80, survivors: 3);
            var view = AttachCageView(cage);

            Assert.AreEqual("80/80", view.HpText.text);
            Assert.AreEqual("+3", view.SurvivorText.text);

            yield return ShootSingleProjectileAndWaitHit(damage: 30);
            Assert.AreEqual("50/80", view.HpText.text);
            Assert.AreEqual("+3", view.SurvivorText.text);

            yield return ShootSingleProjectileAndWaitHit(damage: 50);
            Assert.AreEqual("0", view.HpText.text);
            Assert.AreEqual("RESCUED", view.SurvivorText.text);
        }

        private IEnumerator ShootSingleProjectileAndWaitHit(int damage)
        {
            var projectile = SpawnProjectile(Vector3.zero, damage, ProjectileSpeed);
            yield return WaitForProjectileRecycleOrTimeout(projectile);
            Assert.IsFalse(projectile.IsActiveInPool, "Projetil disparado deve ser reciclado.");
        }

        private IEnumerator WaitForProjectileRecycleOrTimeout(Projectile projectile)
        {
            float elapsed = 0f;
            while (elapsed < TestTimeout && projectile.IsActiveInPool)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
        }

        private IEnumerator WaitForEnemyHitOrTimeout(EnemyController enemy, int expectedHealth)
        {
            float elapsed = 0f;
            while (elapsed < TestTimeout && enemy.CurrentHealth > expectedHealth)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();
        }

        private Cage CreateTestCage(Vector3 position, int hp, int survivors, ISquad squad = null, IEventBus eventBus = null)
        {
            var cageGo = new GameObject("TestCage");
            _spawnedObjects.Add(cageGo);
            cageGo.transform.position = position;

            var cage = cageGo.AddComponent<Cage>();
            cage.Initialize((Game.Data.CageDefinition)null, lane: 0, squad: squad, eventBus: eventBus, hpOverride: hp, survivorsOverride: survivors);
            return cage;
        }

        private CageView AttachCageView(Cage cage)
        {
            var viewGo = new GameObject("TestCageView");
            _spawnedObjects.Add(viewGo);
            viewGo.transform.SetParent(cage.transform, false);

            var hpTextGo = new GameObject("HpText");
            _spawnedObjects.Add(hpTextGo);
            hpTextGo.transform.SetParent(viewGo.transform, false);
            var hpText = hpTextGo.AddComponent<TextMeshProUGUI>();

            var survivorTextGo = new GameObject("SurvivorText");
            _spawnedObjects.Add(survivorTextGo);
            survivorTextGo.transform.SetParent(viewGo.transform, false);
            var survivorText = survivorTextGo.AddComponent<TextMeshProUGUI>();

            var view = viewGo.AddComponent<CageView>();
            view.HpText = hpText;
            view.SurvivorText = survivorText;
            view.Bind(cage);
            return view;
        }

        private Projectile SpawnProjectile(Vector3 position, int damage, float speed)
        {
            var projectileGo = new GameObject("TestProjectile");
            _spawnedObjects.Add(projectileGo);
            projectileGo.transform.position = position;

            var collider = projectileGo.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;

            var projectile = projectileGo.AddComponent<Projectile>();
            projectile.Initialize(damage, speed, maxDistance: 30f, onRecycle: null);
            return projectile;
        }

        private EnemyController CreateTestEnemy(Vector3 position, int initialHealth)
        {
            var enemyGo = new GameObject("TestEnemy");
            _spawnedObjects.Add(enemyGo);
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

        private sealed class TestEventBus : IEventBus
        {
            public readonly List<SurvivorsRescuedEvent> RescuedEvents = new List<SurvivorsRescuedEvent>();
            public readonly List<SurvivorsLostEvent> LostEvents = new List<SurvivorsLostEvent>();

            public void Publish<T>(T eventData)
            {
                if (eventData is SurvivorsRescuedEvent rescued)
                {
                    RescuedEvents.Add(rescued);
                }
                else if (eventData is SurvivorsLostEvent lost)
                {
                    LostEvents.Add(lost);
                }
            }

            public System.IDisposable Subscribe<T>(System.Action<T> handler) => null;
        }
    }
}
