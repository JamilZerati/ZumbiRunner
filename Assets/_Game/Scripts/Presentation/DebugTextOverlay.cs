using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Diagnostics;
using Game.Core.Events;
using Game.Gameplay;
using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation
{
    public class DebugTextOverlay : MonoBehaviour
    {
        private const int MaxRecentGates = 4;
        private const int MaxRecentSynergies = 3;

        [SerializeField] private TMP_Text debugText;
        [SerializeField] private SquadController squadController;
        [SerializeField] private CombatDirector combatDirector;
        [SerializeField] private TrackScroller trackScroller;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private GeneralAbilityController abilityController;

        private IEventBus _eventBus;
        private IDisposable _gateSub;
        private IDisposable _synergySub;
        private IDisposable _progressSub;
        private IDisposable _usedSub;

        private readonly List<GateTriggeredEvent> _recentGates = new List<GateTriggeredEvent>(MaxRecentGates);
        private readonly List<SynergyTriggeredEvent> _recentSynergies = new List<SynergyTriggeredEvent>(MaxRecentSynergies);

        private int _grenadeKills;
        private int _grenadeTargetKills = 25;
        private int _grenadeCharges;
        private int _grenadeUses;

        public bool IsVisible { get; private set; } = true;
        public TMP_Text DebugText => debugText;
        internal Func<bool> DebugBuildCheck { get; set; } = () => Debug.isDebugBuild || Application.isEditor;

        public void ConfigureComponents(TMP_Text text)
        {
            debugText = text;
        }

        public void Initialize(
            IEventBus eventBus,
            SquadController squad = null,
            CombatDirector director = null,
            TrackScroller scroller = null,
            WeaponController weapon = null,
            GeneralAbilityController ability = null,
            TMP_Text text = null)
        {
            Unsubscribe();

            _eventBus = eventBus;
            if (squad != null) squadController = squad;
            if (director != null) combatDirector = director;
            if (scroller != null) trackScroller = scroller;
            if (weapon != null) weaponController = weapon;
            if (ability != null) abilityController = ability;
            if (text != null) debugText = text;

            if (_eventBus != null)
            {
                _gateSub = _eventBus.Subscribe<GateTriggeredEvent>(OnGateTriggered);
                _synergySub = _eventBus.Subscribe<SynergyTriggeredEvent>(OnSynergyTriggered);
                _progressSub = _eventBus.Subscribe<AbilityChargeProgressEvent>(OnChargeProgress);
                _usedSub = _eventBus.Subscribe<AbilityUsedEvent>(OnAbilityUsed);
            }

            if (abilityController != null)
            {
                _grenadeKills = abilityController.CurrentKills;
                _grenadeTargetKills = abilityController.KillsPerCharge;
                _grenadeCharges = abilityController.CurrentCharges;
            }
        }

        public void CheckAndApplyBuildMode()
        {
            if (DebugBuildCheck != null && !DebugBuildCheck())
            {
                gameObject.SetActive(false);
                enabled = false;
            }
        }

        private void Awake()
        {
            CheckAndApplyBuildMode();
        }

        private void Update()
        {
            CheckToggleInput();

            if (IsVisible)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (debugText == null)
            {
                return;
            }

            var effectiveScroller = trackScroller != null ? trackScroller : (combatDirector != null ? combatDirector.Scroller : null);
            var effectiveSquad = squadController != null ? squadController : (combatDirector != null ? combatDirector.Squad : null);

            var data = new DebugOverlayData
            {
                SquadCount = effectiveSquad != null ? effectiveSquad.SquadCount : 0,
                DistanceTraveled = effectiveScroller != null ? effectiveScroller.DistanceTraveled : 0f,
                VictoryDistance = combatDirector != null ? combatDirector.VictoryDistance : 0f,
                WeaponId = weaponController != null ? weaponController.EquippedWeaponId : string.Empty,
                WeaponStats = weaponController != null ? weaponController.CurrentStats : default,
                ActiveStatuses = weaponController != null && weaponController.OnHitStatuses != null ? weaponController.OnHitStatuses.Items : null,
                Stats = weaponController != null ? weaponController.Stats : null,
                RecentGates = _recentGates,
                RecentSynergies = _recentSynergies,
                GrenadeKills = abilityController != null ? abilityController.CurrentKills : _grenadeKills,
                GrenadeTargetKills = abilityController != null ? abilityController.KillsPerCharge : (_grenadeTargetKills > 0 ? _grenadeTargetKills : 25),
                GrenadeCharges = abilityController != null ? abilityController.CurrentCharges : _grenadeCharges,
                GrenadeMode = abilityController != null && abilityController.TriggerPolicy != null ? abilityController.TriggerPolicy.Mode : HeroAbilityTriggerMode.Auto,
                GrenadeUses = _grenadeUses
            };

            debugText.text = DebugOverlayFormatter.Format(in data);
        }

        public void Toggle()
        {
            SetVisible(!IsVisible);
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            if (debugText != null)
            {
                debugText.gameObject.SetActive(visible);
            }
        }

        private void CheckToggleInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                Toggle();
                return;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                int pressedCount = 0;
                bool anyJustPressed = false;
                var touches = touchscreen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed)
                    {
                        pressedCount++;
                        if (touches[i].press.wasPressedThisFrame)
                        {
                            anyJustPressed = true;
                        }
                    }
                }

                if (pressedCount >= 3 && anyJustPressed)
                {
                    Toggle();
                }
            }
        }

        private void OnGateTriggered(GateTriggeredEvent evt)
        {
            if (_recentGates.Count >= MaxRecentGates)
            {
                _recentGates.RemoveAt(0);
            }
            _recentGates.Add(evt);
        }

        private void OnSynergyTriggered(SynergyTriggeredEvent evt)
        {
            if (_recentSynergies.Count >= MaxRecentSynergies)
            {
                _recentSynergies.RemoveAt(0);
            }
            _recentSynergies.Add(evt);
        }

        private void OnChargeProgress(AbilityChargeProgressEvent evt)
        {
            _grenadeKills = evt.CurrentKills;
            _grenadeTargetKills = evt.TargetKills;
            _grenadeCharges = evt.CurrentCharges;
        }

        private void OnAbilityUsed(AbilityUsedEvent evt)
        {
            _grenadeUses++;
            _grenadeCharges = evt.RemainingCharges;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (_gateSub != null) { _gateSub.Dispose(); _gateSub = null; }
            if (_synergySub != null) { _synergySub.Dispose(); _synergySub = null; }
            if (_progressSub != null) { _progressSub.Dispose(); _progressSub = null; }
            if (_usedSub != null) { _usedSub.Dispose(); _usedSub = null; }
        }
    }
}
