using System.Collections.Generic;
using System.Reflection;
using Game.Core;
using Game.Core.Events;
using Game.Infrastructure;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SoldierViewTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private GameObject leaderObject;
        private GameObject controllerObject;
        private SquadVisualController visualController;
        private EventBus eventBus;
        private ObjectPool<SoldierView> soldierPool;

        [SetUp]
        public void SetUp()
        {
            leaderObject = CreateTrackedGameObject("Leader");
            controllerObject = CreateTrackedGameObject("SquadVisualController");
            visualController = controllerObject.AddComponent<SquadVisualController>();
            eventBus = new EventBus();

            soldierPool = new ObjectPool<SoldierView>(
                factory: () =>
                {
                    var go = CreateTrackedGameObject("SoldierViewInstance");
                    return go.AddComponent<SoldierView>();
                },
                onRent: s => s.gameObject.SetActive(true),
                onReturn: s => s.gameObject.SetActive(false)
            );
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }
            createdObjects.Clear();
        }

        private GameObject CreateTrackedGameObject(string name)
        {
            var go = new GameObject(name);
            createdObjects.Add(go);
            return go;
        }

        [Test]
        public void SetTargetOffset_SetsTargetOffsetProperty()
        {
            var go = CreateTrackedGameObject("Soldier");
            var soldier = go.AddComponent<SoldierView>();
            var expectedOffset = new Vector3(1.5f, 0f, -2.0f);

            soldier.SetTargetOffset(expectedOffset);

            Assert.AreEqual(expectedOffset, soldier.TargetOffset);
        }

        [Test]
        public void SnapToTarget_ImmediatelySetsPositionToLeaderPlusOffset()
        {
            var go = CreateTrackedGameObject("Soldier");
            var soldier = go.AddComponent<SoldierView>();
            soldier.transform.position = Vector3.zero;
            soldier.SetTargetOffset(new Vector3(1f, 0f, -3f));

            var leaderPosition = new Vector3(10f, 0f, 20f);
            soldier.SnapToTarget(leaderPosition);

            var expectedPosition = new Vector3(11f, 0f, 17f);
            Assert.AreEqual(expectedPosition, soldier.transform.position);
        }

        [Test]
        public void UpdatePosition_MovesTowardsTargetByFollowSpeedDelta()
        {
            var go = CreateTrackedGameObject("Soldier");
            var soldier = go.AddComponent<SoldierView>();
            soldier.transform.position = Vector3.zero;
            soldier.SetTargetOffset(new Vector3(10f, 0f, 0f));

            var leaderPosition = Vector3.zero;
            soldier.UpdatePosition(leaderPosition, deltaTime: 0.1f, followSpeed: 20f);

            Assert.AreEqual(new Vector3(2f, 0f, 0f), soldier.transform.position);
        }

        [Test]
        public void UpdatePosition_DoesNotOvershootTargetPosition()
        {
            var go = CreateTrackedGameObject("Soldier");
            var soldier = go.AddComponent<SoldierView>();
            soldier.transform.position = Vector3.zero;
            soldier.SetTargetOffset(new Vector3(1f, 0f, 0f));

            var leaderPosition = Vector3.zero;
            soldier.UpdatePosition(leaderPosition, deltaTime: 1f, followSpeed: 20f);

            Assert.AreEqual(new Vector3(1f, 0f, 0f), soldier.transform.position);
        }

        [Test]
        public void Initialize_ConfiguresPropertiesAndSubscribesToEventBus()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus, soldierSpacing: 0.75f, soldiersPerRow: 3);

            Assert.AreEqual(0.75f, visualController.Spacing);
            Assert.AreEqual(3, visualController.MaxPerRow);
            Assert.AreEqual(0, visualController.ActiveSoldiers.Count);
        }

        [Test]
        public void SynchronizeSquad_IncreasesActiveSoldiers_AndRentsFromPool()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus, soldierSpacing: 0.5f, soldiersPerRow: 5);

            visualController.SynchronizeSquad(3);

            Assert.AreEqual(3, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(3, soldierPool.CountActive);

            var expectedPositions = FormationSolver.CalculatePositions(3, 0.5f, 5);
            for (int i = 0; i < 3; i++)
            {
                var soldier = visualController.ActiveSoldiers[i];
                Assert.IsTrue(soldier.gameObject.activeSelf);
                Assert.AreEqual(new Vector3(expectedPositions[i].X, 0f, expectedPositions[i].Z), soldier.TargetOffset);
            }
        }

        [Test]
        public void SynchronizeSquad_DecreasesActiveSoldiers_AndReturnsToPool()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus, soldierSpacing: 0.5f, soldiersPerRow: 5);

            visualController.SynchronizeSquad(5);
            Assert.AreEqual(5, visualController.ActiveSoldiers.Count);

            visualController.SynchronizeSquad(2);
            Assert.AreEqual(2, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(2, soldierPool.CountActive);
            Assert.AreEqual(3, soldierPool.CountInactive);

            var expectedPositions = FormationSolver.CalculatePositions(2, 0.5f, 5);
            for (int i = 0; i < 2; i++)
            {
                Assert.AreEqual(new Vector3(expectedPositions[i].X, 0f, expectedPositions[i].Z), visualController.ActiveSoldiers[i].TargetOffset);
            }
        }

        [Test]
        public void SynchronizeSquad_WithZeroOrNegative_ReturnsAllSoldiersToPool()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus);

            visualController.SynchronizeSquad(4);
            Assert.AreEqual(4, visualController.ActiveSoldiers.Count);

            visualController.SynchronizeSquad(-1);
            Assert.AreEqual(0, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(0, soldierPool.CountActive);
            Assert.AreEqual(4, soldierPool.CountInactive);
        }

        [Test]
        public void SquadSizeChangedEvent_SynchronizesSquadAutomatically()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus);

            eventBus.Publish(new SquadSizeChangedEvent(0, 4));

            Assert.AreEqual(4, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(4, soldierPool.CountActive);

            eventBus.Publish(new SquadSizeChangedEvent(4, 1));

            Assert.AreEqual(1, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(1, soldierPool.CountActive);
        }

        [Test]
        public void Initialize_CalledMultipleTimes_ReplacesEventBusSubscription()
        {
            var bus1 = new EventBus();
            var bus2 = new EventBus();

            visualController.Initialize(leaderObject.transform, soldierPool, bus1);
            visualController.Initialize(leaderObject.transform, soldierPool, bus2);

            bus1.Publish(new SquadSizeChangedEvent(0, 5));
            Assert.AreEqual(0, visualController.ActiveSoldiers.Count);

            bus2.Publish(new SquadSizeChangedEvent(0, 3));
            Assert.AreEqual(3, visualController.ActiveSoldiers.Count);
        }

        [Test]
        public void UpdatePositions_UpdatesPositionsOfAllActiveSoldiers()
        {
            leaderObject.transform.position = new Vector3(5f, 0f, 10f);
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus);
            visualController.SynchronizeSquad(1);

            var soldier = visualController.ActiveSoldiers[0];
            soldier.transform.position = Vector3.zero;

            visualController.UpdatePositions(0.1f);

            Assert.AreNotEqual(Vector3.zero, soldier.transform.position);
        }

        [Test]
        public void OnDestroy_ReturnsAllActiveSoldiersToPoolAndUnsubscribes()
        {
            visualController.Initialize(leaderObject.transform, soldierPool, eventBus);
            visualController.SynchronizeSquad(3);
            Assert.AreEqual(3, soldierPool.CountActive);

            typeof(SquadVisualController)
                .GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(visualController, null);

            Assert.AreEqual(0, visualController.ActiveSoldiers.Count);
            Assert.AreEqual(0, soldierPool.CountActive);
            Assert.AreEqual(3, soldierPool.CountInactive);

            Assert.DoesNotThrow(() => eventBus.Publish(new SquadSizeChangedEvent(3, 6)));
            Assert.AreEqual(0, visualController.ActiveSoldiers.Count);
        }
    }
}
