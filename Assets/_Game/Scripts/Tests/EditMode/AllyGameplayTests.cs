using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Perks;
using Game.Core.Perks.Effects;
using Game.Core.Stats;
using Game.Core.Status;
using Game.Data;
using Game.Gameplay;
using Game.Gameplay.Allies;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode
{
    public class AllyGameplayTests
    {
        private readonly List<Object> _toDestroy = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _toDestroy.Count; i++) if (_toDestroy[i] != null) Object.DestroyImmediate(_toDestroy[i]);
            _toDestroy.Clear();
        }

        private T Track<T>(T obj) where T : Object { if (obj != null) _toDestroy.Add(obj); return obj; }

        private sealed class FakeSettings : IAbilitySettings
        {
            public HeroAbilityTriggerMode TriggerMode { get; set; } = HeroAbilityTriggerMode.Auto;
            public HeroAbilityTriggerMode LoadTriggerMode() => TriggerMode;
            public void SaveTriggerMode(HeroAbilityTriggerMode mode) => TriggerMode = mode;
        }

        private sealed class FakeTarget : IStatusReceiver
        {
            public int Health { get; private set; } = 100;
            public bool IsAlive => Health > 0;
            public DamageInfo LastDamage { get; private set; }
            public IReadOnlyList<StatusApplication> LastApplications { get; private set; }

            public int ReceiveHit(DamageInfo damage, IReadOnlyList<StatusApplication> applications)
            {
                LastDamage = damage;
                LastApplications = applications;
                Health = Math.Max(0, Health - damage.Amount);
                return damage.Amount;
            }
        }

        [Test]
        public void AbilityRunner_Tick_DecrementsCooldownAndUpdatesFillAmount()
        {
            var runner = new AbilityRunner("Inferno", cooldownSeconds: 10f, initialCooldown: 10f);
            Assert.IsFalse(runner.IsReady);
            Assert.AreEqual(0f, runner.FillAmount, 0.001f);
            runner.Tick(4f);
            Assert.AreEqual(6f, runner.CooldownRemaining, 0.001f);
            Assert.AreEqual(0.4f, runner.FillAmount, 0.001f);
            runner.Tick(6f);
            Assert.AreEqual(0f, runner.CooldownRemaining, 0.001f);
            Assert.IsTrue(runner.IsReady);
            Assert.AreEqual(1f, runner.FillAmount, 0.001f);
            runner.Tick(2f);
            Assert.AreEqual(0f, runner.CooldownRemaining, 0.001f);
        }

        [Test]
        public void AbilityRunner_TryTrigger_ReturnsFalse_WhenNotReady()
        {
            var runner = new AbilityRunner("Blizzard", cooldownSeconds: 10f, initialCooldown: 5f);
            var policy = new AutoHeroAbilityTriggerPolicy();
            var context = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);
            bool fired = false;
            Assert.IsFalse(runner.TryTrigger(context, policy, () => fired = true));
            Assert.IsFalse(fired);
            Assert.AreEqual(5f, runner.CooldownRemaining, 0.001f);
        }

        [Test]
        public void AbilityRunner_TryTrigger_AutoPolicy_RequiresTargetsInLane()
        {
            var runner = new AbilityRunner("Inferno", cooldownSeconds: 8f, initialCooldown: 0f);
            var policy = new AutoHeroAbilityTriggerPolicy();
            var noTargets = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: false, manualTriggerRequested: false);
            bool fired = false;
            Assert.IsFalse(runner.TryTrigger(noTargets, policy, () => fired = true));
            Assert.IsFalse(fired);
            var withTargets = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: false);
            Assert.IsTrue(runner.TryTrigger(withTargets, policy, () => fired = true));
            Assert.IsTrue(fired);
            Assert.AreEqual(8f, runner.CooldownRemaining, 0.001f);
        }

        [Test]
        public void AllyHudView_InitializesAndReflectsAbilityState()
        {
            var hero = Track(new GameObject("Hero_Test")).AddComponent<HeroUnit>();
            var runner = new AbilityRunner("Inferno", cooldownSeconds: 10f, initialCooldown: 5f);
            hero.Initialize("pyromancer", "Piromante", maxHealth: 100, ability: runner);
            var hudGo = Track(new GameObject("AllyHud"));
            var hud = hudGo.AddComponent<AllyHudView>();
            var radial = Track(new GameObject("RadialFill")).AddComponent<Image>();
            var btn = Track(new GameObject("ManualBtn")).AddComponent<Button>();
            var modeText = Track(new GameObject("ModeText")).AddComponent<TextMeshProUGUI>();
            var nameText = Track(new GameObject("NameText")).AddComponent<TextMeshProUGUI>();
            var auraGo = Track(new GameObject("AuraBadge"));
            hud.ConfigureComponents(radial, btn, null, modeText, null, nameText, auraGo);
            var settings = new FakeSettings { TriggerMode = HeroAbilityTriggerMode.Manual };
            hud.Initialize(hero, settings);
            Assert.AreEqual(HeroAbilityTriggerMode.Manual, hud.CurrentMode);
            Assert.AreEqual("Inferno", nameText.text);
            Assert.AreEqual(0.5f, radial.fillAmount, 0.001f);
            Assert.IsFalse(btn.interactable);
            runner.Tick(5f);
            hud.UpdateVisuals();
            Assert.AreEqual(1f, radial.fillAmount, 0.001f);
            Assert.IsTrue(btn.interactable);
            hud.ToggleMode();
            Assert.AreEqual(HeroAbilityTriggerMode.Auto, hud.CurrentMode);
            hud.SetPetAuraActive(true);
            Assert.IsTrue(auraGo.activeSelf);
            hud.SetPetAuraActive(false);
            Assert.IsFalse(auraGo.activeSelf);
        }

        [Test]
        public void AllyHudView_TryTriggerManual_FiresAbilityAndDisablesUntilRecharged()
        {
            var hero = Track(new GameObject("Hero_TestManual")).AddComponent<HeroUnit>();
            var runner = new AbilityRunner("Nevasca", cooldownSeconds: 10f, initialCooldown: 0f);
            hero.Initialize("cryomancer", "Criomante", maxHealth: 100, ability: runner, abilityDamage: 50, statusApplied: "freeze");
            var hud = Track(new GameObject("AllyHud_Manual")).AddComponent<AllyHudView>();
            var radial = Track(new GameObject("RadialFill")).AddComponent<Image>();
            var btn = Track(new GameObject("ManualBtn")).AddComponent<Button>();
            hud.ConfigureComponents(radial, btn);
            hud.Initialize(hero, new FakeSettings { TriggerMode = HeroAbilityTriggerMode.Manual });
            var target = new FakeTarget();
            Assert.IsTrue(hud.TryTriggerManual(target));
            Assert.AreEqual(50, target.LastDamage.Amount);
            Assert.IsFalse(runner.IsReady);
            Assert.IsFalse(btn.interactable);
            Assert.AreEqual(0f, radial.fillAmount, 0.001f);
            Assert.IsFalse(hud.TryTriggerManual(target));
        }

        private PerkDefinition CreatePerk(string id, params IPerkEffect[] effects)
        {
            var perk = Track(ScriptableObject.CreateInstance<PerkDefinition>());
            perk.SetData(id, id, new List<IPerkEffect>(effects));
            return perk;
        }

        private GatePair CreatePair(PerkDefinition lane0Perk, PerkDefinition lane1Perk)
        {
            var pair = Track(new GameObject("GatePair")).AddComponent<GatePair>();
            Track(new GameObject("Gate0")).AddComponent<Gate>().Initialize(0, lane0Perk, pair);
            Track(new GameObject("Gate1")).AddComponent<Gate>().Initialize(1, lane1Perk, pair);
            return pair;
        }

        [Test]
        public void GatePair_TryTrigger_WithRecruitAllyPerk_RecruitsAllyIntoRoster()
        {
            var roster = new AllyRoster();
            var pair = CreatePair(CreatePerk("recruit_pyromancer", new RecruitAllyPerkEffect("pyromancer")),
                                  CreatePerk("recruit_guard_dog", new RecruitAllyPerkEffect("guard_dog")));
            var squad = Track(new GameObject("Squad")).AddComponent<SquadController>();
            squad.Initialize(3);

            bool triggered = pair.TryTrigger(0, squad, roster: roster);

            Assert.IsTrue(triggered);
            Assert.IsTrue(pair.IsConsumed);
            Assert.IsTrue(roster.ContainsAlly("pyromancer"));
            Assert.AreEqual(1, roster.HeroCount);
            Assert.AreEqual(0, roster.PetCount);
        }

        [Test]
        public void GatePair_TryTrigger_WithDuplicateRecruitAllyPerk_RestoresAllyHealth()
        {
            var roster = new AllyRoster();
            roster.TryRecruit("pyromancer", out var ally);
            ally.TakeDamage(60);
            Assert.AreEqual(90, ally.CurrentHealth);

            var pair = CreatePair(CreatePerk("recruit_pyromancer", new RecruitAllyPerkEffect("pyromancer")),
                                  CreatePerk("add_5", new AddSoldiersEffect(5)));
            var squad = Track(new GameObject("Squad")).AddComponent<SquadController>();
            squad.Initialize(5);

            bool triggered = pair.TryTrigger(0, squad, roster: roster);

            Assert.IsTrue(triggered);
            Assert.AreEqual(ally.MaxHealth, ally.CurrentHealth);
            Assert.AreEqual(1, roster.Allies.Count);
        }

        [Test]
        public void Gate_OnTriggerEnter_WithSquadController_ResolvesRosterAndRecruitsAlly()
        {
            var general = Track(new GameObject("General"));
            var squad = general.AddComponent<SquadController>();
            squad.Initialize(4);
            var collider = Track(new GameObject("GeneralCollider")).AddComponent<BoxCollider>();
            collider.transform.SetParent(general.transform, false);

            var pair = CreatePair(CreatePerk("recruit_guard_dog", new RecruitAllyPerkEffect("guard_dog")),
                                  CreatePerk("add_5", new AddSoldiersEffect(5)));
            pair.Gates[0].OnTriggerEnter(collider);

            Assert.IsTrue(pair.IsConsumed);
            Assert.AreEqual(1, squad.PetCount);
            Assert.IsTrue(squad.ContainsAlly("guard_dog"));
        }

        [Test]
        public void SquadController_ImplementsIAllyRoster_AndTracksHeroesAndPets()
        {
            var squad = Track(new GameObject("Squad_Roster")).AddComponent<SquadController>();
            squad.Initialize(3);

            Assert.IsInstanceOf<IAllyRoster>(squad);
            Assert.AreEqual(0, squad.HeroCount);
            Assert.AreEqual(0, squad.PetCount);

            Assert.IsTrue(squad.TryRecruit("pyromancer", out var pyro));
            Assert.AreEqual("pyromancer", pyro.Id);
            Assert.AreEqual(1, squad.HeroCount);

            Assert.IsTrue(squad.TryRecruit("guard_dog", out var dog));
            Assert.AreEqual("guard_dog", dog.Id);
            Assert.AreEqual(1, squad.PetCount);
            Assert.AreEqual(2, squad.Allies.Count);
        }

        [Test]
        public void SquadController_CalculatesAllyFlankOffsets_PositioningOnLateralWings()
        {
            var squadGo = Track(new GameObject("Squad_Flank"));
            squadGo.transform.position = new Vector3(0f, 0f, 10f);
            var squad = squadGo.AddComponent<SquadController>();

            Vector3 flank0 = SquadController.GetAllyFlankOffset(0);
            Vector3 flank1 = SquadController.GetAllyFlankOffset(1);

            Assert.Less(flank0.x, 0f, "Primeiro aliado deve se posicionar na ala lateral esquerda.");
            Assert.Greater(flank1.x, 0f, "Segundo aliado deve se posicionar na ala lateral direita.");

            var allyTransform = Track(new GameObject("AllyVisual")).transform;
            squad.PositionAlly(allyTransform, 0);
            Assert.AreEqual(squadGo.transform.position + flank0, allyTransform.position);
        }

        [Test]
        public void SquadVisualController_PositionsAlliesOnFlanks_WhenRegistered()
        {
            var leader = Track(new GameObject("Leader"));
            leader.transform.position = new Vector3(2f, 0f, 5f);
            var visual = Track(new GameObject("Visual")).AddComponent<SquadVisualController>();
            visual.Initialize(leader.transform, null);

            var ally0 = Track(new GameObject("Ally0")).transform;
            var ally1 = Track(new GameObject("Ally1")).transform;

            visual.RegisterAlly(ally0);
            visual.RegisterAlly(ally1);
            Assert.AreEqual(2, visual.ActiveAllies.Count);

            visual.UpdatePositions(0.016f);
            Assert.AreEqual(leader.transform.position + SquadController.GetAllyFlankOffset(0), ally0.position);
            Assert.AreEqual(leader.transform.position + SquadController.GetAllyFlankOffset(1), ally1.position);

            visual.UnregisterAlly(ally0);
            Assert.AreEqual(1, visual.ActiveAllies.Count);
        }

        [Test]
        public void E2E_GatePairRecruitment_CombatDamage_ManualHudTrigger_AndPetAuraIntegration()
        {
            var bus = new EventBus();
            bool damagedEventPublished = false, knockoutEventPublished = false, abilityEventPublished = false;
            bus.Subscribe<AllyDamagedEvent>(_ => damagedEventPublished = true);
            bus.Subscribe<AllyKnockedOutEvent>(_ => knockoutEventPublished = true);
            bus.Subscribe<AllyAbilityFiredEvent>(_ => abilityEventPublished = true);

            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 100f);

            var general = Track(new GameObject("General_E2E"));
            var squad = general.AddComponent<SquadController>();
            squad.Initialize(10, bus);
            var collider = Track(new GameObject("Collider")).AddComponent<BoxCollider>();
            collider.transform.SetParent(general.transform, false);

            var hero = Track(new GameObject("Hero_Pyro")).AddComponent<HeroUnit>();
            var ability = new AbilityRunner("Inferno", cooldownSeconds: 10f, initialCooldown: 0f);
            hero.Initialize("pyromancer", "Piromante", maxHealth: 100, attackDamage: 25, attackInterval: 1.2f, statusApplied: "burn", ability: ability, abilityDamage: 40, bus: bus);
            squad.TryAddAlly(hero, out _);

            var pet = Track(new GameObject("Pet_Dog")).AddComponent<PetUnit>();
            pet.Initialize("guard_dog", "Cão de Guarda", maxHealth: 100, bonus: 0.15f);
            squad.TryAddAlly(pet, out _);
            pet.AttachAura(stats);
            Assert.AreEqual(115f, stats.GetValue(StatId.Damage), 0.001f);

            var hud = Track(new GameObject("Hud_E2E")).AddComponent<AllyHudView>();
            var radial = Track(new GameObject("RadialFill")).AddComponent<Image>();
            var btn = Track(new GameObject("ManualBtn")).AddComponent<Button>();
            hud.ConfigureComponents(radial, btn);
            hud.Initialize(hero, new FakeSettings { TriggerMode = HeroAbilityTriggerMode.Manual }, bus);

            var target = new FakeTarget();
            Assert.IsTrue(hud.TryTriggerManual(target));
            Assert.IsTrue(abilityEventPublished);
            Assert.AreEqual(40, target.LastDamage.Amount);
            Assert.AreEqual(10f, ability.CooldownRemaining, 0.001f);
            Assert.IsFalse(btn.interactable);

            hero.TakeDamage(40);
            Assert.IsTrue(damagedEventPublished);
            Assert.AreEqual(60, hero.CurrentHealth);
            Assert.AreEqual(10, squad.SquadCount);

            hero.TakeDamage(60);
            Assert.IsTrue(knockoutEventPublished);
            Assert.IsFalse(hero.IsAlive);
            Assert.AreEqual(10, squad.SquadCount);
        }

        [Test]
        public void AllyRoster_PublishesAllyRecruitedEvent_OnRecruitAndOnHealthRestore()
        {
            var bus = new EventBus();
            var events = new List<AllyRecruitedEvent>();
            bus.Subscribe<AllyRecruitedEvent>(evt => events.Add(evt));

            var roster = new AllyRoster(bus: bus);
            bool firstAdded = roster.TryRecruit("pyromancer", out var hero);
            Assert.IsTrue(firstAdded);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual("pyromancer", events[0].AllyId);
            Assert.IsFalse(events[0].IsPet);
            Assert.IsFalse(events[0].WasRestored);

            hero.TakeDamage(30);
            bool duplicateAdded = roster.TryRecruit("pyromancer", out _);
            Assert.IsFalse(duplicateAdded);
            Assert.AreEqual(2, events.Count);
            Assert.AreEqual("pyromancer", events[1].AllyId);
            Assert.IsTrue(events[1].WasRestored);
            Assert.AreEqual(hero.MaxHealth, hero.CurrentHealth);
        }
    }
}
