using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class CombatEngagementPlayModeTests
    {
        private readonly List<GameObject> _createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null)
                {
                    Object.Destroy(_createdObjects[i]);
                }
            }
            _createdObjects.Clear();
        }

        private GameObject CreateGameObject(string name)
        {
            var go = new GameObject(name);
            _createdObjects.Add(go);
            return go;
        }

        [UnityTest]
        public IEnumerator EngagedZombie_IsShotPointBlank_AndKilledQuickly()
        {
            var squadGo = CreateGameObject("Squad");
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);

            var combatGo = CreateGameObject("CombatDirector");
            var combat = combatGo.AddComponent<CombatDirector>();
            var melee = squadGo.AddComponent<MeleeEngagementManager>();
            melee.Initialize(squad, combat);
            combat.Initialize(squad, null, null, 100f);

            var enemyGo = CreateGameObject("Enemy");
            var enemyCol = enemyGo.AddComponent<BoxCollider>();
            enemyCol.isTrigger = true;
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, maxHealth: 15, moveSpeed: 2f, null);
            enemy.ContactDPS = 5f;

            combat.ResolveEnemyContact(enemy);
            Assert.IsTrue(enemy.IsEngaged, "Zumbi deve estar engajado");
            Assert.AreEqual(1, melee.EngagedEnemies.Count);

            enemy.TakeDamage(new DamageInfo(20, DamageType.Physical, null));
            yield return null;

            melee.Tick(0.1f);
            Assert.AreEqual(0, melee.EngagedEnemies.Count, "Zumbi morto deve ter desengajado");
            Assert.AreEqual(10, squad.SquadCount, "Nenhum soldado deve ter sido perdido");

            Object.Destroy(squadGo);
            Object.Destroy(combatGo);
            Object.Destroy(enemyGo);
        }

        [UnityTest]
        public IEnumerator ComebackMechanic_PerkGateBoostsSquad_AndOvercomesAttrition()
        {
            var squadGo = CreateGameObject("Squad");
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(1);

            var combatGo = CreateGameObject("CombatDirector");
            var combat = combatGo.AddComponent<CombatDirector>();
            var melee = squadGo.AddComponent<MeleeEngagementManager>();
            melee.Initialize(squad, combat);
            combat.Initialize(squad, null, null, 100f);

            var enemyGo = CreateGameObject("EliteEnemy");
            var enemy = enemyGo.AddComponent<EnemyController>();
            enemy.Initialize(0, maxHealth: 50, moveSpeed: 2f, null);
            enemy.ContactDPS = 20f;

            melee.Engage(enemy);

            melee.Tick(0.6f);
            Assert.AreEqual(0, squad.SquadCount, "Soldado deve ter tombado");
            Assert.IsTrue(melee.HealthBuffer.IsGeneralAlive, "General ainda deve estar vivo");

            squad.Add(10);
            Assert.AreEqual(10, squad.SquadCount, "Tropa recebeu reforço de +10!");

            enemy.TakeDamage(new DamageInfo(50, DamageType.Physical, null));
            yield return null;

            melee.Tick(0.1f);
            Assert.AreEqual(0, melee.EngagedEnemies.Count, "Zumbi derrotado!");
            Assert.IsFalse(combat.IsResolved, "General sobreviveu e segue correndo!");

            Object.Destroy(squadGo);
            Object.Destroy(combatGo);
            Object.Destroy(enemyGo);
        }
    }
}
