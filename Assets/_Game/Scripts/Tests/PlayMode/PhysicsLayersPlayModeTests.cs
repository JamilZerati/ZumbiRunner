using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class PhysicsLayersPlayModeTests
    {
        private List<GameObject> createdObjects;

        [SetUp]
        public void SetUp()
        {
            createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.Destroy(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        private class TriggerRecorder : MonoBehaviour
        {
            public readonly List<Collider> TriggeredColliders = new List<Collider>();

            private void OnTriggerEnter(Collider other)
            {
                TriggeredColliders.Add(other);
            }
        }

        [Test]
        public void CollisionLayers_ConstantesDefinemValoresCorretos()
        {
            Assert.AreEqual(8, CollisionLayers.SquadBodyLayer);
            Assert.AreEqual(9, CollisionLayers.PlayerProjectileLayer);
            Assert.AreEqual(10, CollisionLayers.EnemyLayer);
            Assert.AreEqual(11, CollisionLayers.EnemyProjectileLayer);
            Assert.AreEqual(12, CollisionLayers.PickupLayer);

            Assert.AreEqual(1 << 8, CollisionLayers.SquadBodyMask);
            Assert.AreEqual(1 << 9, CollisionLayers.PlayerProjectileMask);
            Assert.AreEqual(1 << 10, CollisionLayers.EnemyMask);
            Assert.AreEqual(1 << 11, CollisionLayers.EnemyProjectileMask);
            Assert.AreEqual(1 << 12, CollisionLayers.PickupMask);
        }

        [Test]
        public void MatrizDeColisao_PlayerProjectile_ColideComEnemyEPickup_EIgnoraDemais()
        {
            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.EnemyLayer),
                "PlayerProjectile DEVE colidir com Enemy na matriz de física 3D.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.PickupLayer),
                "PlayerProjectile DEVE colidir com Pickup na matriz de física 3D.");

            Assert.IsTrue(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.SquadBodyLayer),
                "PlayerProjectile NÃO DEVE colidir com SquadBody (evita consumo acidental do líder).");

            Assert.IsTrue(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.PlayerProjectileLayer),
                "PlayerProjectile NÃO DEVE colidir com outro PlayerProjectile.");

            Assert.IsTrue(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.EnemyProjectileLayer),
                "PlayerProjectile NÃO DEVE colidir com EnemyProjectile.");
        }

        [Test]
        public void MatrizDeColisao_SquadBody_ColideComEnemyEnemyProjectileEPickup_EIgnoraDemais()
        {
            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.EnemyLayer),
                "SquadBody DEVE colidir com Enemy.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.EnemyProjectileLayer),
                "SquadBody DEVE colidir com EnemyProjectile.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.PickupLayer),
                "SquadBody DEVE colidir com Pickup.");

            Assert.IsTrue(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.SquadBodyLayer),
                "SquadBody NÃO DEVE colidir com outro SquadBody.");
        }

        [Test]
        public void MatrizDeColisao_Enemy_NaoColideComEnemy()
        {
            Assert.IsTrue(
                Physics.GetIgnoreLayerCollision(CollisionLayers.EnemyLayer, CollisionLayers.EnemyLayer),
                "Enemy NÃO DEVE colidir com outro Enemy na física 3D.");
        }

        [Test]
        public void Projectile_Initialize_ConfiguraCamadaERigidbodyCinematico()
        {
            var go = new GameObject("TestProjectile");
            createdObjects.Add(go);
            var proj = go.AddComponent<Projectile>();
            proj.Initialize(10, 15f, 40f, null);

            Assert.AreEqual(CollisionLayers.PlayerProjectileLayer, go.layer);
            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.IsTrue(rb.isKinematic);
            Assert.IsFalse(rb.useGravity);
        }

        [Test]
        public void EnemyController_Initialize_ConfiguraCamadaERigidbodyCinematico()
        {
            var go = new GameObject("TestEnemy");
            createdObjects.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(0, 20, 2f, null);

            Assert.AreEqual(CollisionLayers.EnemyLayer, go.layer);
            var rb = go.GetComponent<Rigidbody>();
            Assert.IsNotNull(rb);
            Assert.IsTrue(rb.isKinematic);
            Assert.IsFalse(rb.useGravity);
        }

        [Test]
        public void SoldierView_EnsurePhysicsSetup_ConfiguraCamadaSquadBodyECollider()
        {
            var go = new GameObject("TestSoldier");
            createdObjects.Add(go);
            var soldier = go.AddComponent<SoldierView>();
            soldier.EnsurePhysicsSetup();

            Assert.AreEqual(CollisionLayers.SquadBodyLayer, go.layer);
            var col = go.GetComponent<Collider>();
            Assert.IsNotNull(col);
            Assert.IsTrue(col.isTrigger);
        }

        [UnityTest]
        public IEnumerator SimulacaoFisica_PlayerProjectile_NaoDisparaTriggerEmSquadBody()
        {
            var squadObj = new GameObject("SquadCollider");
            createdObjects.Add(squadObj);
            squadObj.layer = CollisionLayers.SquadBodyLayer;
            var squadCol = squadObj.AddComponent<SphereCollider>();
            squadCol.isTrigger = true;
            squadCol.radius = 1f;

            var projObj = new GameObject("ProjectileCollider");
            createdObjects.Add(projObj);
            projObj.layer = CollisionLayers.PlayerProjectileLayer;
            var projCol = projObj.AddComponent<SphereCollider>();
            projCol.isTrigger = true;
            projCol.radius = 0.5f;
            var rb = projObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            projObj.transform.position = squadObj.transform.position;

            yield return new WaitForFixedUpdate();

            bool ignore = Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.SquadBodyLayer);
            Assert.IsTrue(ignore, "Camadas PlayerProjectile e SquadBody devem se ignorar mutuamente.");
        }

        [UnityTest]
        public IEnumerator SimulacaoFisica_PlayerProjectile_AcertaEnemy_SemTocarSquadBody()
        {
            var squadObj = new GameObject("SquadObj");
            createdObjects.Add(squadObj);
            squadObj.layer = CollisionLayers.SquadBodyLayer;
            var squadCol = squadObj.AddComponent<SphereCollider>();
            squadCol.isTrigger = true;
            squadCol.radius = 1f;
            var squadTracker = squadObj.AddComponent<TriggerRecorder>();

            var enemyObj = new GameObject("EnemyObj");
            createdObjects.Add(enemyObj);
            enemyObj.layer = CollisionLayers.EnemyLayer;
            var enemyCol = enemyObj.AddComponent<SphereCollider>();
            enemyCol.isTrigger = true;
            enemyCol.radius = 1f;
            var enemyTracker = enemyObj.AddComponent<TriggerRecorder>();

            var projObj = new GameObject("ProjectileObj");
            createdObjects.Add(projObj);
            projObj.layer = CollisionLayers.PlayerProjectileLayer;
            var projCol = projObj.AddComponent<SphereCollider>();
            projCol.isTrigger = true;
            projCol.radius = 0.5f;
            var projRb = projObj.AddComponent<Rigidbody>();
            projRb.isKinematic = true;
            projRb.useGravity = false;

            projObj.transform.position = squadObj.transform.position;
            enemyObj.transform.position = squadObj.transform.position + Vector3.forward * 5f;

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, squadTracker.TriggeredColliders.Count, "SquadBody NÃO deve receber trigger de PlayerProjectile.");

            projObj.transform.position = enemyObj.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, squadTracker.TriggeredColliders.Count, "SquadBody continua sem triggers de PlayerProjectile.");
            Assert.AreEqual(1, enemyTracker.TriggeredColliders.Count, "Enemy DEVE receber trigger de PlayerProjectile.");
            Assert.AreEqual(projCol, enemyTracker.TriggeredColliders[0]);
        }

        [UnityTest]
        public IEnumerator SimulacaoFisica_Enemy_TocaSquadBody_AcionaTrigger()
        {
            var squadObj = new GameObject("SquadObj");
            createdObjects.Add(squadObj);
            squadObj.layer = CollisionLayers.SquadBodyLayer;
            var squadCol = squadObj.AddComponent<SphereCollider>();
            squadCol.isTrigger = true;
            squadCol.radius = 1f;
            var squadTracker = squadObj.AddComponent<TriggerRecorder>();

            var enemyObj = new GameObject("EnemyObj");
            createdObjects.Add(enemyObj);
            enemyObj.layer = CollisionLayers.EnemyLayer;
            var enemyCol = enemyObj.AddComponent<SphereCollider>();
            enemyCol.isTrigger = true;
            enemyCol.radius = 1f;
            var enemyRb = enemyObj.AddComponent<Rigidbody>();
            enemyRb.isKinematic = true;
            enemyRb.useGravity = false;

            enemyObj.transform.position = squadObj.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, squadTracker.TriggeredColliders.Count, "SquadBody DEVE detectar colisão/trigger com Enemy.");
            Assert.AreEqual(enemyCol, squadTracker.TriggeredColliders[0]);
        }

        [UnityTest]
        public IEnumerator SimulacaoFisica_PlayerProjectile_AcertaPickup_AcionaTrigger()
        {
            var pickupObj = new GameObject("PickupObj");
            createdObjects.Add(pickupObj);
            pickupObj.layer = CollisionLayers.PickupLayer;
            var pickupCol = pickupObj.AddComponent<SphereCollider>();
            pickupCol.isTrigger = true;
            pickupCol.radius = 1f;
            var pickupTracker = pickupObj.AddComponent<TriggerRecorder>();

            var projObj = new GameObject("ProjectileObj");
            createdObjects.Add(projObj);
            projObj.layer = CollisionLayers.PlayerProjectileLayer;
            var projCol = projObj.AddComponent<SphereCollider>();
            projCol.isTrigger = true;
            projCol.radius = 0.5f;
            var projRb = projObj.AddComponent<Rigidbody>();
            projRb.isKinematic = true;
            projRb.useGravity = false;

            projObj.transform.position = pickupObj.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, pickupTracker.TriggeredColliders.Count, "Pickup DEVE detectar trigger de PlayerProjectile.");
            Assert.AreEqual(projCol, pickupTracker.TriggeredColliders[0]);
        }

        [UnityTest]
        public IEnumerator SimulacaoFisica_SquadBody_TocaPickup_AcionaTrigger()
        {
            var pickupObj = new GameObject("PickupObj");
            createdObjects.Add(pickupObj);
            pickupObj.layer = CollisionLayers.PickupLayer;
            var pickupCol = pickupObj.AddComponent<SphereCollider>();
            pickupCol.isTrigger = true;
            pickupCol.radius = 1f;
            var pickupTracker = pickupObj.AddComponent<TriggerRecorder>();

            var squadObj = new GameObject("SquadObj");
            createdObjects.Add(squadObj);
            squadObj.layer = CollisionLayers.SquadBodyLayer;
            var squadCol = squadObj.AddComponent<SphereCollider>();
            squadCol.isTrigger = true;
            squadCol.radius = 1f;
            var squadRb = squadObj.AddComponent<Rigidbody>();
            squadRb.isKinematic = true;
            squadRb.useGravity = false;

            squadObj.transform.position = pickupObj.transform.position;

            yield return new WaitForFixedUpdate();

            Assert.AreEqual(1, pickupTracker.TriggeredColliders.Count, "Pickup DEVE detectar trigger de SquadBody.");
            Assert.AreEqual(squadCol, pickupTracker.TriggeredColliders[0]);
        }
    }
}
