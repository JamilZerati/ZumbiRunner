using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class PlatoonCombatTests
    {
        private List<GameObject> spawnedObjects;
        private EventBus eventBus;
        private ObjectPool<Projectile> pool;

        [SetUp]
        public void SetUp()
        {
            spawnedObjects = new List<GameObject>();
            eventBus = new EventBus();

            pool = new ObjectPool<Projectile>(() =>
            {
                var go = new GameObject("PooledProjectile");
                spawnedObjects.Add(go);
                var p = go.AddComponent<Projectile>();
                return p;
            });
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < spawnedObjects.Count; i++)
            {
                if (spawnedObjects[i] != null)
                {
                    Object.DestroyImmediate(spawnedObjects[i]);
                }
            }
            spawnedObjects.Clear();
        }

        private WeaponController CreateWeapon(ISquad squad, int baseDamage = 2, int projectileCount = 1)
        {
            var go = new GameObject("TestWeapon");
            spawnedObjects.Add(go);
            var weapon = go.AddComponent<WeaponController>();
            weapon.DamagePerShot = baseDamage;
            weapon.Initialize(pool, squad, null, eventBus);

            if (projectileCount > 1)
            {
                weapon.Stats.SetBase(StatId.ProjectileCount, projectileCount);
            }

            return weapon;
        }

        private (CombatDirector director, SquadController squad, GameStateMachine stateMachine, TrackScroller scroller)
            CreateCombatContext(int initialSquadCount)
        {
            var directorGo = new GameObject("Director");
            spawnedObjects.Add(directorGo);
            var director = directorGo.AddComponent<CombatDirector>();

            var squadGo = new GameObject("Squad");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(initialSquadCount, eventBus);

            var scrollerGo = new GameObject("Scroller");
            spawnedObjects.Add(scrollerGo);
            var scroller = scrollerGo.AddComponent<TrackScroller>();
            scroller.ForwardSpeed = 10f;

            var sm = new GameStateMachine(null, GameState.Boot);
            sm.TryTransition(GameState.Run);

            director.Initialize(squad, scroller, sm, 120f, null, eventBus);
            return (director, squad, sm, scroller);
        }

        private EnemyController CreateEnemy(int contactCost = 1, string archetypeId = "walker", int health = 20)
        {
            var go = new GameObject("TestEnemy");
            spawnedObjects.Add(go);
            var enemy = go.AddComponent<EnemyController>();
            enemy.Initialize(0, health, 2f, null);
            enemy.ContactCost = contactCost;
            enemy.ArchetypeId = archetypeId;
            return enemy;
        }

        // ==========================================
        // 1. Cenários de Disparo por Pelotão
        // ==========================================

        [Test]
        public void FireVolley_ComPelotoes_DanoDoProjetilMultiplicaSoldadosDoPelotao()
        {
            // Tropa 10 -> 2 emissores de 5 soldados cada. Dano base da arma = 2.
            // Cada projétil disparado deve carregar 2 * 5 = 10 de dano.
            var squadGo = new GameObject("Squad");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);

            var weapon = CreateWeapon(squad, baseDamage: 2);
            var volley = weapon.FireVolley();

            Assert.AreEqual(2, volley.Count, "Deveria disparar 2 projéteis (1 por emissor de pelotão).");
            Assert.AreEqual(10, volley[0].Damage, "Projétil do pelotão 0 deve ter dano 10 (2 base * 5 soldados).");
            Assert.AreEqual(10, volley[1].Damage, "Projétil do pelotão 1 deve ter dano 10 (2 base * 5 soldados).");
        }

        [Test]
        public void FireVolley_ComPelotoesDesiguais_CadaProjetilTemDanoDoSeuPelotao()
        {
            // Tropa 7 -> ceil(7/5) = 2 emissores. Pelotão 0: 4 soldados. Pelotão 1: 3 soldados.
            // Dano base = 2 -> Projétil 0: 8 de dano; Projétil 1: 6 de dano.
            var squadGo = new GameObject("Squad");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(7);

            var weapon = CreateWeapon(squad, baseDamage: 2);
            var volley = weapon.FireVolley();

            Assert.AreEqual(2, volley.Count);
            Assert.AreEqual(8, volley[0].Damage, "Projétil do pelotão 0 deve ter 8 de dano (2 * 4 soldados).");
            Assert.AreEqual(6, volley[1].Damage, "Projétil do pelotão 1 deve ter 6 de dano (2 * 3 soldados).");
        }

        [Test]
        public void FireVolley_ComPelotoes_PosicionaProjeteisNosOffsetsDosEmissores()
        {
            // Tropa 10 -> 2 emissores. As posições dos projéteis devem acompanhar a formação geométrica.
            var squadGo = new GameObject("Squad");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);

            var weapon = CreateWeapon(squad, baseDamage: 2);
            weapon.transform.position = new Vector3(5f, 0f, 20f);

            var volley = weapon.FireVolley();

            Assert.AreEqual(2, volley.Count);
            // Os 2 emissores para 10 soldados estão distribuídos simetricamente no eixo X
            Assert.Less(volley[0].transform.position.x, weapon.transform.position.x);
            Assert.Greater(volley[1].transform.position.x, weapon.transform.position.x);
            // Emissores ficam atrás do líder (offset Z negativo em relação à frente da arma)
            Assert.AreEqual(volley[0].transform.position.z, volley[1].transform.position.z, 0.001f);
        }

        [Test]
        public void FireVolley_DanoTotalDaSalvaEscalaLinearmenteComTropa()
        {
            // Dano base = 2.
            // Tropa 10 -> dano total = 2 * 10 = 20.
            // Tropa 20 -> dano total = 2 * 20 = 40.
            var squadGo1 = new GameObject("Squad10");
            spawnedObjects.Add(squadGo1);
            var squad10 = squadGo1.AddComponent<SquadController>();
            squad10.Initialize(10);

            var weapon10 = CreateWeapon(squad10, baseDamage: 2);
            var volley10 = weapon10.FireVolley();
            int totalDamage10 = volley10.Sum(p => p.Damage);
            Assert.AreEqual(20, totalDamage10);

            var squadGo2 = new GameObject("Squad20");
            spawnedObjects.Add(squadGo2);
            var squad20 = squadGo2.AddComponent<SquadController>();
            squad20.Initialize(20);

            var weapon20 = CreateWeapon(squad20, baseDamage: 2);
            var volley20 = weapon20.FireVolley();
            int totalDamage20 = volley20.Sum(p => p.Damage);
            Assert.AreEqual(40, totalDamage20);
        }

        [Test]
        public void FireVolley_SemSquad_UsaFallbackDeUmEmissorComDanoBase()
        {
            var weapon = CreateWeapon(null, baseDamage: 2);
            var volley = weapon.FireVolley();

            Assert.AreEqual(1, volley.Count);
            Assert.AreEqual(2, volley[0].Damage);
        }

        [Test]
        public void FireVolley_ComSquadZerado_UsaFallbackDeUmEmissorComDanoBase()
        {
            var squadGo = new GameObject("SquadZero");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(0);

            var weapon = CreateWeapon(squad, baseDamage: 2);
            var volley = weapon.FireVolley();

            Assert.AreEqual(1, volley.Count);
            Assert.AreEqual(2, volley[0].Damage);
        }

        [Test]
        public void FireVolley_ComShotgunTresProjeteisEDoisPelotoes_DisparaSeisProjeteisTotais()
        {
            // Tropa 10 -> 2 emissores. Arma com 3 projéteis por disparo -> 2 * 3 = 6 projéteis no total.
            var squadGo = new GameObject("SquadShotgun");
            spawnedObjects.Add(squadGo);
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);

            var weapon = CreateWeapon(squad, baseDamage: 2, projectileCount: 3);
            var volley = weapon.FireVolley();

            Assert.AreEqual(6, volley.Count);
            // Cada projétil do pelotão de 5 soldados tem dano 2 * 5 = 10
            foreach (var proj in volley)
            {
                Assert.AreEqual(10, proj.Damage);
            }
        }

        // ==========================================
        // 2. Cenários de Contato e Consumo de Zumbis
        // ==========================================

        [Test]
        public void ResolveEnemyContact_ComTropaSuficiente_ConsomeSoldadosPublicaEnemyConsumedERecicla()
        {
            var (director, squad, sm, _) = CreateCombatContext(initialSquadCount: 10);
            var enemy = CreateEnemy(contactCost: 2, archetypeId: "walker");

            EnemyConsumedEvent consumedEvent = default;
            bool eventFired = false;
            eventBus.Subscribe<EnemyConsumedEvent>(evt =>
            {
                consumedEvent = evt;
                eventFired = true;
            });

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.AreEqual(8, squad.SquadCount, "Deve consumir exatamente 2 soldados da tropa.");
            Assert.IsTrue(eventFired, "Deve publicar EnemyConsumedEvent no EventBus.");
            Assert.AreEqual("walker", consumedEvent.ArchetypeId);
            Assert.AreEqual(2, consumedEvent.SoldiersLost);
            Assert.IsFalse(enemy.IsActiveInPool, "Inimigo deve ser reciclado.");
            Assert.IsFalse(director.IsResolved);
            Assert.AreEqual(GameState.Run, sm.CurrentState);
        }

        [Test]
        public void ResolveEnemyContact_NaoPublicaEntityDiedEvent()
        {
            var (director, squad, _, _) = CreateCombatContext(initialSquadCount: 10);
            var enemy = CreateEnemy(contactCost: 1, archetypeId: "walker");

            bool diedEventFired = false;
            eventBus.Subscribe<EntityDiedEvent>(_ => diedEventFired = true);

            director.ResolveEnemyContact(enemy);

            Assert.IsFalse(diedEventFired, "Consumo por contato NÃO é abate e não deve disparar EntityDiedEvent.");
        }

        [Test]
        public void ResolveEnemyContact_QuandoContatoZeraTropa_GeneralPermaneceVivoESemDerrota()
        {
            // Tropa inicial de 2 soldados. Inimigo com ContactCost = 2 consome os últimos 2 soldados.
            // Invariante M15: O General sobrevive sozinho no corredor; derrota NÃO ocorre ainda.
            var (director, squad, sm, scroller) = CreateCombatContext(initialSquadCount: 2);
            var enemy = CreateEnemy(contactCost: 2, archetypeId: "walker");

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.AreEqual(0, squad.SquadCount, "Tropa deve zerar.");
            Assert.IsFalse(director.IsResolved, "Combate NÃO deve ser encerrado ainda (General sozinho).");
            Assert.IsFalse(scroller.IsPaused, "Scroller deve continuar rodando.");
            Assert.AreEqual(GameState.Run, sm.CurrentState, "Estado de jogo deve permanecer em Run.");
        }

        [Test]
        public void ResolveEnemyContact_ComTropaZerada_ContatoSubsequenteCausaDerrota()
        {
            // Tropa já zerada (General sozinho). Próximo contato físico resulta em derrota imediata.
            var (director, squad, sm, scroller) = CreateCombatContext(initialSquadCount: 0);
            var enemy = CreateEnemy(contactCost: 1, archetypeId: "runner");

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsTrue(result);
            Assert.IsTrue(director.IsResolved, "Combate deve ser resolvido como derrota.");
            Assert.IsTrue(scroller.IsPaused, "Scroller deve ser pausado.");
            Assert.AreEqual(GameState.Defeat, sm.CurrentState, "Estado deve transicionar para Defeat.");
        }

        [Test]
        public void ResolveEnemyContact_QuandoDanoExcedeTropa_SoldiersLostLimitaAoRestante()
        {
            // Tropa de 1 soldado, zumbi com ContactCost de 3 soldados.
            // SoldiersLost reportado deve ser 1 (quantidade real perdida), não 3.
            var (director, squad, _, _) = CreateCombatContext(initialSquadCount: 1);
            var enemy = CreateEnemy(contactCost: 3, archetypeId: "tank");

            EnemyConsumedEvent consumedEvent = default;
            eventBus.Subscribe<EnemyConsumedEvent>(evt => consumedEvent = evt);

            director.ResolveEnemyContact(enemy);

            Assert.AreEqual(0, squad.SquadCount);
            Assert.AreEqual(1, consumedEvent.SoldiersLost, "SoldiersLost deve ser limitado aos soldados existentes.");
            Assert.AreEqual("tank", consumedEvent.ArchetypeId);
        }

        [Test]
        public void ResolveEnemyContact_ComInimigoInativo_RetornaFalseSemConsumirTropa()
        {
            var (director, squad, _, _) = CreateCombatContext(initialSquadCount: 5);
            var enemy = CreateEnemy();
            enemy.Recycle(); // desativa o inimigo

            bool eventFired = false;
            eventBus.Subscribe<EnemyConsumedEvent>(_ => eventFired = true);

            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsFalse(result);
            Assert.AreEqual(5, squad.SquadCount);
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void ResolveEnemyContact_AposDerrota_IgnoraContatosSubsequentes()
        {
            var (director, _, sm, _) = CreateCombatContext(initialSquadCount: 0);
            director.TriggerDefeat();

            var enemy = CreateEnemy();
            bool result = director.ResolveEnemyContact(enemy);

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Defeat, sm.CurrentState);
        }
    }
}
