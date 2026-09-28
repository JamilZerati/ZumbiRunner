using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Data;
using Game.Gameplay;
using Game.Infrastructure;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class StatAndWeaponPerkTests
    {
        private const float Tolerance = 0.0001f;

        private List<Object> toDestroy;

        [SetUp]
        public void SetUp()
        {
            toDestroy = new List<Object>();
        }

        [TearDown]
        public void TearDown()
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

        private T Track<T>(T obj) where T : Object
        {
            toDestroy.Add(obj);
            return obj;
        }

        private PerkDefinition CreatePerk(string id, params IPerkEffect[] effects)
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData(id, id, new List<IPerkEffect>(effects));
            return perk;
        }

        private WeaponController CreateLoadout(string initialWeaponId = "pistol")
        {
            var go = Track(new GameObject("Weapon"));
            var weapon = go.AddComponent<WeaponController>();
            weapon.Initialize(new ObjectPool<Projectile>(() => Track(new GameObject("P")).AddComponent<Projectile>()),
                              null, WeaponTestCatalog.CreateDefault());
            weapon.TryEquip(initialWeaponId);
            return weapon;
        }

        private GatePair CreatePair(PerkDefinition lane0Perk, PerkDefinition lane1Perk)
        {
            var pair = Track(new GameObject("GatePair")).AddComponent<GatePair>();
            Track(new GameObject("Gate0")).AddComponent<Gate>().Initialize(0, lane0Perk, pair);
            Track(new GameObject("Gate1")).AddComponent<Gate>().Initialize(1, lane1Perk, pair);
            return pair;
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
                SquadCount += amount;
                return true;
            }

            public bool Remove(int amount)
            {
                SquadCount = Mathf.Max(0, SquadCount - amount);
                return true;
            }

            public bool Multiply(int factor)
            {
                SquadCount *= factor;
                return true;
            }

            public bool Divide(int divisor)
            {
                SquadCount /= divisor;
                return true;
            }

            public bool SetCount(int newCount)
            {
                SquadCount = newCount;
                return true;
            }
        }

        [Test]
        public void PerkContext_Constructor_StoresLoadoutAndSourceId()
        {
            var loadout = CreateLoadout();

            var context = new PerkContext(null, 1, loadout, "damage_up_25");

            Assert.AreSame(loadout, context.Loadout);
            Assert.AreEqual("damage_up_25", context.SourceId);
            Assert.AreEqual(1, context.LaneIndex);
        }

        [Test]
        public void PerkContext_LegacyConstructor_HasNoLoadoutAndNoSourceId()
        {
            var context = new PerkContext(new FakeSquad(3), 1);

            Assert.IsNull(context.Loadout);
            Assert.IsNull(context.SourceId);
        }

        [Test]
        public void PerkContext_WithSourceId_ReplacesOnlySourceId()
        {
            var squad = new FakeSquad(3);
            var loadout = CreateLoadout();

            var context = new PerkContext(squad, 1, loadout).WithSourceId("fire_rate_up_1");

            Assert.AreSame(squad, context.Squad);
            Assert.AreEqual(1, context.LaneIndex);
            Assert.AreSame(loadout, context.Loadout);
            Assert.AreEqual("fire_rate_up_1", context.SourceId);
        }

        [Test]
        public void ModifyStatEffect_AppliedThroughPerkDefinition_UsesPerkIdAsSource()
        {
            var loadout = CreateLoadout();
            var perk = CreatePerk("damage_up_25", new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f));

            perk.Apply(new PerkContext(null, 0, loadout));

            Assert.AreEqual(13, loadout.CurrentStats.Damage);
            Assert.AreEqual(1, loadout.Stats.RemoveAllFromSource("damage_up_25"));
            Assert.AreEqual(10, loadout.CurrentStats.Damage);
        }

        [Test]
        public void ModifyStatEffect_SamePerkTwice_StacksAndIsRemovedTogether()
        {
            var loadout = CreateLoadout();
            var perk = CreatePerk("damage_up_25", new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f));

            perk.Apply(new PerkContext(null, 0, loadout));
            perk.Apply(new PerkContext(null, 0, loadout));

            Assert.AreEqual(15, loadout.CurrentStats.Damage);
            Assert.AreEqual(2, loadout.Stats.RemoveAllFromSource("damage_up_25"));
        }

        [Test]
        public void PerkDefinitionApply_KeepsSourceIdAlreadyInContext()
        {
            var loadout = CreateLoadout();
            var perk = CreatePerk("damage_up_25", new ModifyStatEffect(StatId.Damage, ModifierKind.Flat, 5f));

            perk.Apply(new PerkContext(null, 0, loadout, "hero_passive"));

            Assert.AreEqual(1, loadout.Stats.RemoveAllFromSource("hero_passive"));
            Assert.AreEqual(0, loadout.Stats.RemoveAllFromSource("damage_up_25"));
        }

        [Test]
        public void ModifyStatEffect_WithoutSourceId_FallsBackToEffectInstance()
        {
            var loadout = CreateLoadout();
            var effect = new ModifyStatEffect(StatId.FireRate, ModifierKind.Flat, 1f);

            effect.Apply(new PerkContext(null, 0, loadout));

            Assert.AreEqual(3f, loadout.CurrentStats.FireRate, Tolerance);
            Assert.AreEqual(1, loadout.Stats.RemoveAllFromSource(effect));
        }

        [Test]
        public void ModifyStatEffect_WithoutLoadout_IsNoOp()
        {
            var effect = new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f);

            Assert.DoesNotThrow(() => effect.Apply(new PerkContext(new FakeSquad(3))));
        }

        [Test]
        public void EquipWeaponEffect_SwitchesLoadoutWeapon()
        {
            var loadout = CreateLoadout();

            new EquipWeaponEffect("shotgun").Apply(new PerkContext(null, 0, loadout));

            Assert.AreEqual("shotgun", loadout.EquippedWeaponId);
        }

        [Test]
        public void EquipWeaponEffect_UnknownWeapon_KeepsCurrentWeapon()
        {
            var loadout = CreateLoadout();

            new EquipWeaponEffect("bazooka").Apply(new PerkContext(null, 0, loadout));

            Assert.AreEqual("pistol", loadout.EquippedWeaponId);
        }

        [Test]
        public void EquipWeaponEffect_WithoutLoadout_IsNoOp()
        {
            var effect = new EquipWeaponEffect("shotgun");

            Assert.DoesNotThrow(() => effect.Apply(new PerkContext(new FakeSquad(3))));
        }

        [Test]
        public void Descriptions_FollowGateLabelFormat()
        {
            Assert.AreEqual("+25% Dano", new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f).Description);
            Assert.AreEqual("+1 Cadência", new ModifyStatEffect(StatId.FireRate, ModifierKind.Flat, 1f).Description);
            Assert.AreEqual("shotgun", new EquipWeaponEffect("shotgun").Description);
        }

        [Test]
        public void TryTrigger_WeaponGateWithLoadout_SwitchesWeapon()
        {
            var loadout = CreateLoadout();
            var pair = CreatePair(CreatePerk("weapon_shotgun", new EquipWeaponEffect("shotgun")),
                                  CreatePerk("weapon_smg", new EquipWeaponEffect("smg")));

            bool triggered = pair.TryTrigger(0, new FakeSquad(5), loadout: loadout);

            Assert.IsTrue(triggered);
            Assert.AreEqual("shotgun", loadout.EquippedWeaponId);
        }

        [Test]
        public void TryTrigger_StatGateWithLoadout_AddsModifierSourcedByPerkId()
        {
            var loadout = CreateLoadout();
            var pair = CreatePair(CreatePerk("damage_up_25", new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f)),
                                  CreatePerk("fire_rate_up_1", new ModifyStatEffect(StatId.FireRate, ModifierKind.Flat, 1f)));

            pair.TryTrigger(1, new FakeSquad(5), loadout: loadout);

            Assert.AreEqual(3f, loadout.CurrentStats.FireRate, Tolerance);
            Assert.AreEqual(10, loadout.CurrentStats.Damage);
            Assert.AreEqual(1, loadout.Stats.RemoveAllFromSource("fire_rate_up_1"));
        }

        [Test]
        public void TryTrigger_StatGateThenWeaponGate_ModifierSurvivesWeaponSwitch()
        {
            var loadout = CreateLoadout();
            var statPair = CreatePair(CreatePerk("damage_up_25", new ModifyStatEffect(StatId.Damage, ModifierKind.PercentAdd, 0.25f)),
                                      CreatePerk("fire_rate_up_1", new ModifyStatEffect(StatId.FireRate, ModifierKind.Flat, 1f)));
            var weaponPair = CreatePair(CreatePerk("weapon_shotgun", new EquipWeaponEffect("shotgun")),
                                        CreatePerk("weapon_smg", new EquipWeaponEffect("smg")));
            var squad = new FakeSquad(5);

            statPair.TryTrigger(0, squad, loadout: loadout);
            weaponPair.TryTrigger(0, squad, loadout: loadout);

            Assert.AreEqual("shotgun", loadout.EquippedWeaponId);
            Assert.AreEqual(10, loadout.CurrentStats.Damage);
        }

        [Test]
        public void TryTrigger_ConsumedWeaponPair_DoesNotSwitchAgain()
        {
            var loadout = CreateLoadout();
            var pair = CreatePair(CreatePerk("weapon_shotgun", new EquipWeaponEffect("shotgun")),
                                  CreatePerk("weapon_smg", new EquipWeaponEffect("smg")));
            var squad = new FakeSquad(5);

            pair.TryTrigger(0, squad, loadout: loadout);
            bool second = pair.TryTrigger(1, squad, loadout: loadout);

            Assert.IsFalse(second);
            Assert.AreEqual("shotgun", loadout.EquippedWeaponId);
        }

        [Test]
        public void TryTrigger_WithoutLoadout_WeaponEffectIsNoOpAndArithmeticStillApplies()
        {
            var pair = CreatePair(CreatePerk("mixed", new EquipWeaponEffect("shotgun"), new AddSoldiersEffect(5)),
                                  CreatePerk("add_10", new AddSoldiersEffect(10)));
            var squad = new FakeSquad(10);

            bool triggered = pair.TryTrigger(0, squad);

            Assert.IsTrue(triggered);
            Assert.AreEqual(15, squad.SquadCount);
        }

        [Test]
        public void TryTrigger_BusPassedPositionallyAndLoadoutByName_BothWork()
        {
            var loadout = CreateLoadout();
            var pair = CreatePair(CreatePerk("weapon_smg", new EquipWeaponEffect("smg")),
                                  CreatePerk("add_10", new AddSoldiersEffect(10)));
            var bus = new EventBus();
            GateTriggeredEvent? gateEvent = null;
            bus.Subscribe<GateTriggeredEvent>(e => gateEvent = e);

            pair.TryTrigger(0, new FakeSquad(5), bus, loadout: loadout);

            Assert.AreEqual("smg", loadout.EquippedWeaponId);
            Assert.IsTrue(gateEvent.HasValue);
            Assert.AreEqual("weapon_smg", gateEvent.Value.PerkId);
        }

        [Test]
        public void GateOnTriggerEnter_ResolvesLoadoutFromColliderParent_AndSwitchesWeapon()
        {
            var general = Track(new GameObject("General"));
            var squad = general.AddComponent<SquadController>();
            squad.Initialize(3);
            var weapon = general.AddComponent<WeaponController>();
            weapon.Initialize(new ObjectPool<Projectile>(() => Track(new GameObject("P")).AddComponent<Projectile>()),
                              squad, WeaponTestCatalog.CreateDefault());
            weapon.TryEquip("pistol");
            var colliderGo = Track(new GameObject("GeneralCollider"));
            colliderGo.transform.SetParent(general.transform, false);
            var collider = colliderGo.AddComponent<BoxCollider>();

            var pair = CreatePair(CreatePerk("weapon_shotgun", new EquipWeaponEffect("shotgun")),
                                  CreatePerk("weapon_smg", new EquipWeaponEffect("smg")));
            pair.Gates[1].OnTriggerEnter(collider);

            Assert.IsTrue(pair.IsConsumed);
            Assert.AreEqual("smg", weapon.EquippedWeaponId);
        }
    }
}
