using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
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
            // PlayerProjectile deve colidir estritamente com Enemy e Pickup
            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.EnemyLayer),
                "PlayerProjectile DEVE colidir com Enemy na matriz de física 3D.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.PickupLayer),
                "PlayerProjectile DEVE colidir com Pickup na matriz de física 3D.");

            // Armadilha GH #47: PlayerProjectile NUNCA deve colidir com SquadBody
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
            // SquadBody deve colidir com Enemy, EnemyProjectile e Pickup
            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.EnemyLayer),
                "SquadBody DEVE colidir com Enemy.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.EnemyProjectileLayer),
                "SquadBody DEVE colidir com EnemyProjectile.");

            Assert.IsFalse(
                Physics.GetIgnoreLayerCollision(CollisionLayers.SquadBodyLayer, CollisionLayers.PickupLayer),
                "SquadBody DEVE colidir com Pickup.");

            // SquadBody não deve colidir consigo mesmo
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

        [UnityTest]
        public IEnumerator SimulacaoFisica_PlayerProjectile_NaoDisparaTriggerEmSquadBody()
        {
            // Configurar dois objetos sobrepostos nas camadas PlayerProjectile e SquadBody
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

            // Se a matriz estiver configurada corretamente para ignorar, nenhum trigger é gerado
            bool ignore = Physics.GetIgnoreLayerCollision(CollisionLayers.PlayerProjectileLayer, CollisionLayers.SquadBodyLayer);
            Assert.IsTrue(ignore, "Camadas PlayerProjectile e SquadBody devem se ignorar mutuamente.");
        }
    }
}
