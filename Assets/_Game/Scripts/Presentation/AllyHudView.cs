using System;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Allies;
using Game.Core.Events;
using Game.Core.Status;
using Game.Gameplay.Allies;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation
{
    public class AllyHudView : MonoBehaviour
    {
        public const string DefaultSettingsKey = "AllyAbility.TriggerMode";

        [SerializeField] private HeroUnit boundHero;
        [SerializeField] private Image cooldownRadialFill;
        [SerializeField] private Button manualTriggerButton, modeToggleButton;
        [SerializeField] private Toggle modeToggle;
        [SerializeField] private TMP_Text abilityNameText, modeText;
        [SerializeField] private GameObject petAuraBadge;
        [SerializeField] private string autoModeLabel = "Auto", manualModeLabel = "Manual";

        private IAbilitySettings _settings;
        private IHeroAbilityTriggerPolicy _policy;
        private HeroAbilityTriggerMode _currentMode;
        private IEventBus _eventBus;
        private IDisposable _abilityFiredSub, _allyKnockedOutSub;
        private bool _isInitialized;

        public HeroUnit BoundHero => boundHero;
        public Image CooldownRadialFill => cooldownRadialFill;
        public Button ManualTriggerButton => manualTriggerButton;
        public Button ModeToggleButton => modeToggleButton;
        public Toggle ModeToggle => modeToggle;
        public TMP_Text AbilityNameText => abilityNameText;
        public TMP_Text ModeText => modeText;
        public GameObject PetAuraBadge => petAuraBadge;
        public HeroAbilityTriggerMode CurrentMode => _currentMode;
        public IHeroAbilityTriggerPolicy Policy => _policy;
        public IAbilitySettings Settings => _settings;

        public void Initialize(HeroUnit hero, IAbilitySettings settings = null, IEventBus eventBus = null)
        {
            CleanupSubscriptions();
            UnbindUiListeners();
            boundHero = hero;
            _settings = settings ?? new PlayerPrefsAbilitySettings(DefaultSettingsKey);
            _eventBus = eventBus;
            _currentMode = _settings.LoadTriggerMode();
            ApplyMode(_currentMode);
            BindUiListeners();
            SubscribeEvents();
            UpdateVisuals();
            _isInitialized = true;
        }

        public void BindHero(HeroUnit hero) { boundHero = hero; UpdateVisuals(); }

        public void ConfigureComponents(Image radialFill, Button manualBtn = null, Button modeBtn = null,
            TMP_Text modeTxt = null, Toggle toggle = null, TMP_Text nameTxt = null, GameObject auraBadge = null)
        {
            cooldownRadialFill = radialFill;
            manualTriggerButton = manualBtn;
            modeToggleButton = modeBtn;
            modeText = modeTxt;
            modeToggle = toggle;
            abilityNameText = nameTxt;
            petAuraBadge = auraBadge;
        }

        public void SetMode(HeroAbilityTriggerMode mode)
        {
            _currentMode = mode;
            _settings?.SaveTriggerMode(mode);
            ApplyMode(mode);
            UpdateModeVisuals();
        }

        public void ToggleMode() => SetMode(_currentMode == HeroAbilityTriggerMode.Auto ? HeroAbilityTriggerMode.Manual : HeroAbilityTriggerMode.Auto);

        public bool TryTriggerManual(IStatusReceiver target = null)
        {
            if (boundHero == null || !boundHero.IsAlive || boundHero.Ability == null) return false;
            var context = new AbilityTriggerContext(currentCharges: 1, hasTargetsInLane: true, manualTriggerRequested: true);
            bool triggered = boundHero.Ability.TryTrigger(context, _policy, () =>
            {
                if (target != null) boundHero.FireAbility(target);
            });
            UpdateVisuals();
            return triggered;
        }

        public void SetPetAuraActive(bool active) { if (petAuraBadge != null) petAuraBadge.SetActive(active); }

        public void UpdateVisuals()
        {
            if (cooldownRadialFill != null && boundHero != null && boundHero.Ability != null)
                cooldownRadialFill.fillAmount = boundHero.Ability.FillAmount;
            if (manualTriggerButton != null)
                manualTriggerButton.interactable = boundHero != null && boundHero.IsAlive && boundHero.Ability != null && boundHero.Ability.IsReady;
            if (abilityNameText != null)
                abilityNameText.text = boundHero?.Ability?.AbilityName ?? string.Empty;
            UpdateModeVisuals();
        }

        private void ApplyMode(HeroAbilityTriggerMode mode)
        {
            _policy = mode == HeroAbilityTriggerMode.Auto ? (IHeroAbilityTriggerPolicy)new AutoHeroAbilityTriggerPolicy() : new ManualHeroAbilityTriggerPolicy();
        }

        private void UpdateModeVisuals()
        {
            if (modeText != null) modeText.text = _currentMode == HeroAbilityTriggerMode.Auto ? autoModeLabel : manualModeLabel;
            if (modeToggle != null) modeToggle.SetIsOnWithoutNotify(_currentMode == HeroAbilityTriggerMode.Auto);
        }

        private void BindUiListeners()
        {
            if (manualTriggerButton != null) manualTriggerButton.onClick.AddListener(() => TryTriggerManual());
            if (modeToggleButton != null) modeToggleButton.onClick.AddListener(ToggleMode);
            if (modeToggle != null) modeToggle.onValueChanged.AddListener(isAuto => SetMode(isAuto ? HeroAbilityTriggerMode.Auto : HeroAbilityTriggerMode.Manual));
        }

        private void UnbindUiListeners()
        {
            if (manualTriggerButton != null) manualTriggerButton.onClick.RemoveAllListeners();
            if (modeToggleButton != null) modeToggleButton.onClick.RemoveAllListeners();
            if (modeToggle != null) modeToggle.onValueChanged.RemoveAllListeners();
        }

        private void SubscribeEvents()
        {
            if (_eventBus == null) return;
            _abilityFiredSub = _eventBus.Subscribe<AllyAbilityFiredEvent>(_ => UpdateVisuals());
            _allyKnockedOutSub = _eventBus.Subscribe<AllyKnockedOutEvent>(_ => UpdateVisuals());
        }

        private void CleanupSubscriptions()
        {
            _abilityFiredSub?.Dispose();
            _abilityFiredSub = null;
            _allyKnockedOutSub?.Dispose();
            _allyKnockedOutSub = null;
        }

        private void Update() { if (_isInitialized) UpdateVisuals(); }
        private void OnDestroy() { UnbindUiListeners(); CleanupSubscriptions(); }
    }
}
