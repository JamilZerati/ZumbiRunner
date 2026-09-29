using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class ProjectileContactPlayModeTests
    {
        private const int InitialSquad = 3;
        private const int EnemyHealth = 100;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null)
                {
                    Object.Destroy(go);
                }
            }
            _spawned.Clear();

            var pool = GameObject.Find("ProjectilePool");
            if (pool != null)
            {
                Object.Destroy(pool);
            }
        }

        [UnityTest]
        public IEnumerator ProjectileHitOnDistantEnemy_DamagesEnemy_WithoutCostingSoldiers()
        {
            var general = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _spawned.Add(general);
            var rb = general.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var squad = general.AddComponent<SquadController>();
            squad.Initialize(InitialSquad);

            var director = general.AddComponent<CombatDirector>();
            var stateMachine = new GameStateMachine(null, GameState.Boot);
            stateMachine.TryTransition(GameState.Run);
            director.Initialize(squad, null, stateMachine, 1000f);

            var projectileTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _spawned.Add(projectileTemplate);
            projectileTemplate.transform.localScale = Vector3.one * 0.3f;
            projectileTemplate.GetComponent<SphereCollider>().isTrigger = true;
            var projectilePrefab = projectileTemplate.AddComponent<Projectile>();
            projectileTemplate.SetActive(false);

            var weapon = general.AddComponent<WeaponController>();
            weapon.ProjectilePrefab = projectilePrefab;

            var enemyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _spawned.Add(enemyGo);
            enemyGo.transform.position = new Vector3(0f, 0f, 10f);
            enemyGo.GetComponent<CapsuleCollider>().isTrigger = true;
            enemyGo.AddComponent<HealthComponent>();
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, EnemyHealth, 0f, null);

            float elapsed = 0f;
            while (elapsed < 1.5f && enemy.CurrentHealth == EnemyHealth)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForFixedUpdate();

            Assert.Less(enemy.CurrentHealth, EnemyHealth, "Projectile should have hit the enemy at z=10.");
            Assert.AreEqual(InitialSquad, squad.SquadCount, "A projectile hit ten metres away must not cost a soldier.");
            Assert.IsTrue(enemy.IsActiveInPool, "A projectile hit must not recycle a surviving enemy.");
        }
    }
}
