using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Data;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class LivingGatesGameplayTests
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

        private (GateView view, TextMeshProUGUI text, Renderer panel, Renderer frame) CreateTestGateView(GameObject parentGo)
        {
            var view = parentGo.AddComponent<GateView>();

            var textGo = Track(new GameObject("Label"));
            textGo.transform.SetParent(parentGo.transform, false);
            var text = textGo.AddComponent<TextMeshProUGUI>();

            var panelGo = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            panelGo.name = "Panel";
            panelGo.transform.SetParent(parentGo.transform, false);
            var panelRenderer = panelGo.GetComponent<Renderer>();
            panelRenderer.sharedMaterial = Track(new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")));

            var frameGo = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            frameGo.name = "Frame";
            frameGo.transform.SetParent(parentGo.transform, false);
            var frameRenderer = frameGo.GetComponent<Renderer>();
            frameRenderer.sharedMaterial = Track(new Material(Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit")));

            view.LabelText = text;
            view.PanelRenderer = panelRenderer;
            view.FrameRenderer = frameRenderer;

            return (view, text, panelRenderer, frameRenderer);
        }

        private sealed class FakeSquad : ISquad
        {
            public int SquadCount { get; private set; }

            public FakeSquad(int initialCount)
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
        public void CT2_ShootToUpgrade_TakeDamage_AdvancesValueAndClampsAtCap()
        {
            var gateGo = Track(new GameObject("ShootGate"));
            var gate = gateGo.AddComponent<Gate>();
            var (view, text, _, _) = CreateTestGateView(gateGo);

            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 2, hitsPerStep: 1, cap: 15);
            gate.SetVariant(variant);
            view.Initialize(gate);

            Assert.IsTrue(gate.IsAlive);
            Assert.AreEqual(-5, gate.RuntimeState.CurrentValue);
            Assert.AreEqual("-5", text.text);
            Assert.AreEqual(GateView.NegativeColor, view.CurrentColor);

            var dummyDamage = new DamageInfo(10, DamageType.Physical, null);

            for (int i = 0; i < 3; i++)
            {
                gate.TakeDamage(dummyDamage);
            }

            Assert.AreEqual(1, gate.RuntimeState.CurrentValue);
            Assert.AreEqual("+1", text.text);
            Assert.AreEqual(GateView.PositiveColor, view.CurrentColor);

            for (int i = 0; i < 7; i++)
            {
                gate.TakeDamage(dummyDamage);
            }

            Assert.AreEqual(15, gate.RuntimeState.CurrentValue);
            Assert.AreEqual("+15", text.text);

            for (int i = 0; i < 5; i++)
            {
                gate.TakeDamage(dummyDamage);
            }

            Assert.AreEqual(15, gate.RuntimeState.CurrentValue);
            Assert.AreEqual("+15", text.text);
        }

        [Test]
        public void CT3_ShootToUpgrade_TryTrigger_AppliesAccumulatedValueToSquad_RespectingFloorOfOne()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("UpgradeGate"));
            var gate = gateGo.AddComponent<Gate>();
            var variant = new ShootToUpgradeGateVariant(initialValue: -5, step: 1, hitsPerStep: 1, cap: 15);
            gate.SetVariant(variant);
            gate.Initialize(0, (PerkDefinition)null, pair);

            var squad1 = new FakeSquad(10);
            pair.TryTrigger(0, squad1);
            Assert.AreEqual(5, squad1.SquadCount);

            var pairGo2 = Track(new GameObject("GatePair2"));
            var pair2 = pairGo2.AddComponent<GatePair>();
            var gateGo2 = Track(new GameObject("UpgradeGate2"));
            var gate2 = gateGo2.AddComponent<Gate>();
            var variant2 = new ShootToUpgradeGateVariant(initialValue: -2, step: 2, hitsPerStep: 1, cap: 20);
            gate2.SetVariant(variant2);
            gate2.Initialize(0, (PerkDefinition)null, pair2);

            var damage = new DamageInfo(10, DamageType.Physical, null);
            for (int i = 0; i < 5; i++)
            {
                gate2.TakeDamage(damage);
            }
            Assert.AreEqual(8, gate2.RuntimeState.CurrentValue);

            var squad2 = new FakeSquad(10);
            pair2.TryTrigger(0, squad2);
            Assert.AreEqual(18, squad2.SquadCount);

            var pairGo3 = Track(new GameObject("GatePair3"));
            var pair3 = pairGo3.AddComponent<GatePair>();
            var gateGo3 = Track(new GameObject("UpgradeGate3"));
            var gate3 = gateGo3.AddComponent<Gate>();
            gate3.SetVariant(new ShootToUpgradeGateVariant(initialValue: -5, step: 1, hitsPerStep: 1, cap: 15));
            gate3.Initialize(0, (PerkDefinition)null, pair3);

            var squad3 = new FakeSquad(3);
            pair3.TryTrigger(0, squad3);
            Assert.AreEqual(1, squad3.SquadCount);
        }

        [Test]
        public void CT4_SelectiveDamageable_ShootToUpgradeReceivesHits_WhileStaticCursedGuardedAndConsumedPassThrough()
        {
            var pairGo = Track(new GameObject("Pair"));
            var pair = pairGo.AddComponent<GatePair>();

            var shootGateGo = Track(new GameObject("ShootGate"));
            var shootGate = shootGateGo.AddComponent<Gate>();
            shootGate.SetVariant(new ShootToUpgradeGateVariant(initialValue: -5, step: 2, hitsPerStep: 1, cap: 15));
            shootGate.Initialize(0, (PerkDefinition)null, pair);

            var staticGateGo = Track(new GameObject("StaticGate"));
            var staticGate = staticGateGo.AddComponent<Gate>();
            staticGate.SetVariant(new StaticGateVariant());
            staticGate.Initialize(1, (PerkDefinition)null, pair);

            var cursedGateGo = Track(new GameObject("CursedGate"));
            var cursedGate = cursedGateGo.AddComponent<Gate>();
            cursedGate.SetVariant(new CursedGateVariant("perk_bonus", new StageModifierSpec("cursed", 1.2f, 1.1f), 100f));
            cursedGate.Initialize(2, (PerkDefinition)null, pair);

            var guardedGateGo = Track(new GameObject("GuardedGate"));
            var guardedGate = guardedGateGo.AddComponent<Gate>();
            guardedGate.SetVariant(new GuardedGateVariant("wave_1", "runner", 5, 8f));
            guardedGate.Initialize(3, (PerkDefinition)null, pair);

            Assert.IsTrue(shootGate.IsAlive);
            Assert.IsFalse(staticGate.IsAlive);
            Assert.IsFalse(cursedGate.IsAlive);
            Assert.IsFalse(guardedGate.IsAlive);

            var projUpgradeGo = Track(new GameObject("ProjUpgrade"));
            var projUpgrade = projUpgradeGo.AddComponent<Projectile>();
            projUpgrade.Initialize(10, 20f, 50f, null);
            projUpgrade.HandleTrigger(shootGate.gameObject);

            Assert.IsFalse(projUpgrade.IsActiveInPool);
            Assert.AreEqual(-3, shootGate.RuntimeState.CurrentValue);

            var projStaticGo = Track(new GameObject("ProjStatic"));
            var projStatic = projStaticGo.AddComponent<Projectile>();
            projStatic.Initialize(10, 20f, 50f, null);
            projStatic.HandleTrigger(staticGate.gameObject);

            Assert.IsTrue(projStatic.IsActiveInPool);

            var projCursedGo = Track(new GameObject("ProjCursed"));
            var projCursed = projCursedGo.AddComponent<Projectile>();
            projCursed.Initialize(10, 20f, 50f, null);
            projCursed.HandleTrigger(cursedGate.gameObject);

            Assert.IsTrue(projCursed.IsActiveInPool);

            var projGuardedGo = Track(new GameObject("ProjGuarded"));
            var projGuarded = projGuardedGo.AddComponent<Projectile>();
            projGuarded.Initialize(10, 20f, 50f, null);
            projGuarded.HandleTrigger(guardedGate.gameObject);

            Assert.IsTrue(projGuarded.IsActiveInPool);

            pair.TryTrigger(0, new FakeSquad(10));
            Assert.IsTrue(pair.IsConsumed);
            Assert.IsFalse(shootGate.IsAlive);

            var projConsumedGo = Track(new GameObject("ProjConsumed"));
            var projConsumed = projConsumedGo.AddComponent<Projectile>();
            projConsumed.Initialize(10, 20f, 50f, null);
            projConsumed.HandleTrigger(shootGate.gameObject);

            Assert.IsTrue(projConsumed.IsActiveInPool);
        }

        [Test]
        public void CT5_CursedGate_ActivatesStageModifier_AndExpiresAfterDurationDistance()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("CursedGate"));
            var gate = gateGo.AddComponent<Gate>();

            var modifierSpec = new StageModifierSpec("cursed_frenzy", enemySpeedMultiplier: 1.35f, enemyContactDamageMultiplier: 1.2f);
            var cursedVariant = new CursedGateVariant("add_10", modifierSpec, durationDistance: 150f);
            gate.SetVariant(cursedVariant);

            var basePerk = CreatePerk("multiply_2", "x2", new MultiplySoldiersEffect(2));
            var bonusPerk = CreatePerk("add_10", "+10", new AddSoldiersEffect(10));
            gate.Initialize(0, basePerk, pair, bonusPerk);

            var controller = new StageModifierController();
            var squad = new FakeSquad(5);

            pair.TryTrigger(0, squad, currentDistance: 50f, stageModifiers: controller);

            Assert.AreEqual(20, squad.SquadCount);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.35f, controller.CurrentEnemySpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.2f, controller.CurrentEnemyContactDamageMultiplier, 0.0001f);

            controller.TickDistance(100f);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.35f, controller.CurrentEnemySpeedMultiplier, 0.0001f);

            controller.TickDistance(199f);
            Assert.AreEqual(1, controller.ActiveCount);
            Assert.AreEqual(1.35f, controller.CurrentEnemySpeedMultiplier, 0.0001f);

            controller.TickDistance(200f);
            Assert.AreEqual(0, controller.ActiveCount);
            Assert.AreEqual(1.0f, controller.CurrentEnemySpeedMultiplier, 0.0001f);
            Assert.AreEqual(1.0f, controller.CurrentEnemyContactDamageMultiplier, 0.0001f);
        }

        [Test]
        public void CT7_GatePassedEvent_PublishedToEventBus_WithExactValues()
        {
            var pairGo = Track(new GameObject("GatePair"));
            var pair = pairGo.AddComponent<GatePair>();

            var gateGo = Track(new GameObject("UpgradeGate"));
            var gate = gateGo.AddComponent<Gate>();
            gate.GateId = "gate_upgrade_minus5_to_15";

            var basePerk = CreatePerk("add_5", "+5", new AddSoldiersEffect(5));
            var variant = new ShootToUpgradeGateVariant(initialValue: -2, step: 2, hitsPerStep: 1, cap: 20);
            gate.SetVariant(variant);
            gate.Initialize(0, basePerk, pair);

            var damage = new DamageInfo(10, DamageType.Physical, null);
            for (int i = 0; i < 5; i++)
            {
                gate.TakeDamage(damage);
            }
            Assert.AreEqual(8, gate.RuntimeState.CurrentValue);

            var bus = new EventBus();
            GatePassedEvent? passedEvent = null;
            GateTriggeredEvent? triggeredEvent = null;
            bus.Subscribe<GatePassedEvent>(evt => passedEvent = evt);
            bus.Subscribe<GateTriggeredEvent>(evt => triggeredEvent = evt);

            var squad = new FakeSquad(10);
            pair.TryTrigger(0, squad, bus: bus);

            Assert.IsTrue(passedEvent.HasValue);
            Assert.AreEqual("gate_upgrade_minus5_to_15", passedEvent.Value.GateId);
            Assert.AreEqual("add_5", passedEvent.Value.PerkId);
            Assert.AreEqual(GateVariantType.ShootToUpgrade, passedEvent.Value.Variant);
            Assert.AreEqual(0, passedEvent.Value.LaneIndex);
            Assert.AreEqual(8, passedEvent.Value.AppliedValue);

            Assert.IsTrue(triggeredEvent.HasValue);
            Assert.AreEqual(0, triggeredEvent.Value.LaneIndex);
            Assert.AreEqual("add_5", triggeredEvent.Value.PerkId);
        }

        [Test]
        public void GateView_CursedVariant_DisplaysCursedColor()
        {
            var gateGo = Track(new GameObject("CursedGate"));
            var gate = gateGo.AddComponent<Gate>();
            var (view, text, _, _) = CreateTestGateView(gateGo);

            var cursedVariant = new CursedGateVariant("bonus", new StageModifierSpec("frenzy", 1.3f, 1.0f), 100f);
            gate.SetVariant(cursedVariant, "x3");
            view.Initialize(gate);

            Assert.AreEqual("x3", text.text);
            Assert.AreEqual(GateView.CursedColor, view.CurrentColor);
            Assert.IsTrue(view.PanelRenderer.sharedMaterial.color == GateView.CursedColor);
        }
    }
}
