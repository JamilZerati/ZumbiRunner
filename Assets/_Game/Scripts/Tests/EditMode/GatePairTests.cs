using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Data;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class GatePairTests
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
            if (toDestroy != null)
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
        }

        private T Track<T>(T obj) where T : Object
        {
            if (obj != null)
            {
                toDestroy.Add(obj);
            }
            return obj;
        }

        private PerkDefinition CreatePerk(string id, string name, params IPerkEffect[] effects)
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData(id, name, new List<IPerkEffect>(effects));
            return perk;
        }

        private class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount = 0)
            {
                SquadCount = initialCount;
            }

            public bool Add(int amount)
            {
                if (amount <= 0) return false;
                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                if (amount <= 0) return false;
                SquadCount = Mathf.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                if (factor < 0) return false;
                SquadCount = Mathf.Max(0, SquadCount * factor);
                return true;
            }

            public bool Divide(int divisor)
            {
                if (divisor <= 0) return false;
                SquadCount = Mathf.Max(0, SquadCount / divisor);
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = Mathf.Max(0, newCount);
                return true;
            }
        }

        [Test]
        public void TryTrigger_ValidLane_AppliesPerkAndMarksConsumed()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gate0Go = Track(new GameObject("Gate0"));
            var gate0 = gate0Go.AddComponent<Gate>();
            gate0.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            var gate1Go = Track(new GameObject("Gate1"));
            var gate1 = gate1Go.AddComponent<Gate>();
            gate1.Initialize(1, CreatePerk("mult_2", "x2", new MultiplySoldiersEffect(2)), pair);

            var squad = new FakeSquad(10);
            bool success = pair.TryTrigger(0, squad);

            Assert.IsTrue(success);
            Assert.IsTrue(pair.IsConsumed);
            Assert.AreEqual(15, squad.SquadCount);
        }

        [Test]
        public void TryTrigger_WhenAlreadyConsumed_ReturnsFalseAndDoesNotReapply()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gate0Go = Track(new GameObject("Gate0"));
            var gate0 = gate0Go.AddComponent<Gate>();
            gate0.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            var gate1Go = Track(new GameObject("Gate1"));
            var gate1 = gate1Go.AddComponent<Gate>();
            gate1.Initialize(1, CreatePerk("mult_2", "x2", new MultiplySoldiersEffect(2)), pair);

            var squad = new FakeSquad(10);
            Assert.IsTrue(pair.TryTrigger(0, squad));
            Assert.AreEqual(15, squad.SquadCount);

            bool secondAttempt = pair.TryTrigger(1, squad);
            Assert.IsFalse(secondAttempt);
            Assert.AreEqual(15, squad.SquadCount);
        }

        [Test]
        public void TryTrigger_NonExistentLane_ReturnsFalseAndDoesNotConsumePair()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            var squad = new FakeSquad(10);
            bool result = pair.TryTrigger(5, squad);

            Assert.IsFalse(result);
            Assert.IsFalse(pair.IsConsumed);
            Assert.AreEqual(10, squad.SquadCount);
        }

        [Test]
        public void TryTrigger_NullSquad_ReturnsFalseAndDoesNotConsumePair()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            bool result = pair.TryTrigger(0, null);

            Assert.IsFalse(result);
            Assert.IsFalse(pair.IsConsumed);
        }

        [Test]
        public void TryTrigger_PublishesGateTriggeredEventToBus()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate1"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(1, CreatePerk("mult_3", "x3", new MultiplySoldiersEffect(3)), pair);

            var bus = new EventBus();
            GateTriggeredEvent? receivedEvent = null;
            bus.Subscribe<GateTriggeredEvent>(evt => receivedEvent = evt);

            var squad = new FakeSquad(5);
            pair.TryTrigger(1, squad, bus);

            Assert.IsTrue(receivedEvent.HasValue);
            Assert.AreEqual(1, receivedEvent.Value.LaneIndex);
            Assert.AreEqual("mult_3", receivedEvent.Value.PerkId);
        }

        [Test]
        public void TryTrigger_WithEventBusFromInitialize_PublishesEvent()
        {
            var bus = new EventBus();
            GateTriggeredEvent? receivedEvent = null;
            bus.Subscribe<GateTriggeredEvent>(evt => receivedEvent = evt);

            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_2", "+2", new AddSoldiersEffect(2)));

            pair.Initialize(new[] { gate }, bus);

            var squad = new FakeSquad(5);
            pair.TryTrigger(0, squad);

            Assert.IsTrue(receivedEvent.HasValue);
            Assert.AreEqual(0, receivedEvent.Value.LaneIndex);
            Assert.AreEqual("add_2", receivedEvent.Value.PerkId);
        }

        [Test]
        public void TryTrigger_InvokesOnGateTriggeredAction()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate1"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(1, CreatePerk("add_10", "+10", new AddSoldiersEffect(10)), pair);

            int triggeredLane = -1;
            Gate triggeredGate = null;
            pair.OnGateTriggered += (lane, g) =>
            {
                triggeredLane = lane;
                triggeredGate = g;
            };

            var squad = new FakeSquad(5);
            pair.TryTrigger(1, squad);

            Assert.AreEqual(1, triggeredLane);
            Assert.AreSame(gate, triggeredGate);
        }

        [Test]
        public void Gate_OnTriggerEnter_WithSquadCollider_TriggersPair()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_7", "+7", new AddSoldiersEffect(7)), pair);

            var squadGo = Track(new GameObject("Squad"));
            var squadController = squadGo.AddComponent<SquadController>();
            squadController.Initialize(10);
            var squadCollider = squadGo.AddComponent<BoxCollider>();

            gate.OnTriggerEnter(squadCollider);

            Assert.IsTrue(pair.IsConsumed);
            Assert.AreEqual(17, squadController.SquadCount);
        }

        [Test]
        public void Gate_OnTriggerEnter_WhenAlreadyConsumed_DoesNotRetrigger()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            var squadGo = Track(new GameObject("Squad"));
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);
            var collider = squadGo.AddComponent<BoxCollider>();

            gate.OnTriggerEnter(collider);
            Assert.AreEqual(15, squad.SquadCount);

            gate.OnTriggerEnter(collider);
            Assert.AreEqual(15, squad.SquadCount);
        }

        [Test]
        public void Gate_OnTriggerEnter_NonSquadCollider_Ignored()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("add_5", "+5", new AddSoldiersEffect(5)), pair);

            var obstacleGo = Track(new GameObject("Obstacle"));
            var obstacleCollider = obstacleGo.AddComponent<BoxCollider>();

            gate.OnTriggerEnter(obstacleCollider);

            Assert.IsFalse(pair.IsConsumed);
        }

        [Test]
        public void GatePair_IntegrationWithRealSquadController()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("Gate0"));
            var gate = gateGo.AddComponent<Gate>();
            gate.Initialize(0, CreatePerk("mult_3", "x3", new MultiplySoldiersEffect(3)), pair);

            var squadGo = Track(new GameObject("Squad"));
            var squad = squadGo.AddComponent<SquadController>();
            squad.Initialize(10);

            bool success = pair.TryTrigger(0, squad);

            Assert.IsTrue(success);
            Assert.IsTrue(pair.IsConsumed);
            Assert.AreEqual(30, squad.SquadCount);
        }
    }
}
