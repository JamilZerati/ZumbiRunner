using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Status;
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
    }
}
