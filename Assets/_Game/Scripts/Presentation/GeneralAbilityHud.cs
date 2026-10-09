using System;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Events;
using Game.Gameplay.Abilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game.Presentation
{
    public class GeneralAbilityHud : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private GeneralAbilityController controller;
        [SerializeField] private AbilityAimIndicator aimIndicator;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text chargesText;
        [SerializeField] private Button manualTriggerButton;
        [SerializeField] private Button modeToggleButton;
        [SerializeField] private Toggle modeToggle;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private string chargesFormat = "Cargas: {0}";
        [SerializeField] private string autoModeLabel = "Auto";
        [SerializeField] private string manualModeLabel = "Manual";

        private IEventBus _eventBus;
        private IAbilitySettings _settings;
        private IDisposable _progressSub;
        private IDisposable _usedSub;
        private HeroAbilityTriggerMode _currentMode;
        private bool _isInitialized;

        public GeneralAbilityController Controller => controller;
        public Slider ProgressSlider => progressSlider;
        public TMP_Text ChargesText => chargesText;
        public Button ManualTriggerButton => manualTriggerButton;
        public Button ModeToggleButton => modeToggleButton;
        public Toggle ModeToggle => modeToggle;
        public TMP_Text ModeText => modeText;
        public HeroAbilityTriggerMode CurrentMode => _currentMode;
        public IAbilitySettings Settings => _settings;
        public AbilityAimIndicator AimIndicator { get => aimIndicator; set => aimIndicator = value; }
        public int DisplayedCharges { get; private set; }
        public int DisplayedKills { get; private set; }
        public int DisplayedTargetKills { get; private set; }

        public void Initialize(GeneralAbilityController abilityController, IEventBus eventBus = null, IAbilitySettings settings = null)
        {
            CleanupSubscriptions();
            UnbindUiListeners();

            controller = abilityController;
            _eventBus = eventBus;
            _settings = settings ?? new PlayerPrefsAbilitySettings();
            _isInitialized = true;

            _currentMode = _settings.LoadTriggerMode();
            ApplyModeToController(_currentMode);
            UpdateModeVisuals();

            BindUiListeners();

            if (_eventBus != null)
            {
                _progressSub = _eventBus.Subscribe<AbilityChargeProgressEvent>(OnChargeProgress);
                _usedSub = _eventBus.Subscribe<AbilityUsedEvent>(OnAbilityUsed);
            }

            if (controller != null)
            {
                UpdateChargeState(controller.CurrentKills, controller.KillsPerCharge, controller.CurrentCharges);
            }
            else
            {
                UpdateChargeState(0, 25, 0);
            }
        }

        public void ConfigureComponents(
            Slider slider,
            TMP_Text charges,
            Button manualBtn,
            Button modeBtn = null,
            TMP_Text modeTxt = null,
            Toggle toggle = null)
        {
            progressSlider = slider;
            chargesText = charges;
            manualTriggerButton = manualBtn;
            modeToggleButton = modeBtn;
            modeText = modeTxt;
            modeToggle = toggle;
        }

        public void ToggleMode()
        {
            var nextMode = _currentMode == HeroAbilityTriggerMode.Auto
                ? HeroAbilityTriggerMode.Manual
                : HeroAbilityTriggerMode.Auto;
            SetMode(nextMode);
        }

        public void SetMode(HeroAbilityTriggerMode mode)
        {
            _currentMode = mode;
            _settings?.SaveTriggerMode(mode);
            ApplyModeToController(mode);
            UpdateModeVisuals();
        }

        private void ApplyModeToController(HeroAbilityTriggerMode mode)
        {
            if (controller == null)
            {
                return;
            }

            IHeroAbilityTriggerPolicy policy = mode == HeroAbilityTriggerMode.Auto
                ? new AutoHeroAbilityTriggerPolicy()
                : new ManualHeroAbilityTriggerPolicy();

            controller.SetTriggerPolicy(policy);
        }

        private void OnManualButtonClicked()
        {
            if (controller != null)
            {
                var config = controller.Definition?.Targeting;
                if (config == null || config.Type == Game.Core.Abilities.AbilityTargetingType.Instant)
                {
                    controller.TriggerAbility(manual: true);
                }
            }
        }

        private void OnModeToggleChanged(bool isAuto)
        {
            var targetMode = isAuto ? HeroAbilityTriggerMode.Auto : HeroAbilityTriggerMode.Manual;
            if (_currentMode != targetMode)
            {
                SetMode(targetMode);
            }
        }

        private void UpdateModeVisuals()
        {
            if (modeText != null)
            {
                modeText.text = _currentMode == HeroAbilityTriggerMode.Auto ? autoModeLabel : manualModeLabel;
            }

            if (modeToggle != null)
            {
                modeToggle.SetIsOnWithoutNotify(_currentMode == HeroAbilityTriggerMode.Auto);
            }
        }

        private void UpdateChargeState(int currentKills, int targetKills, int currentCharges)
        {
            DisplayedKills = currentKills;
            DisplayedTargetKills = targetKills > 0 ? targetKills : 25;
            DisplayedCharges = currentCharges;

            if (progressSlider != null)
            {
                progressSlider.minValue = 0f;
                progressSlider.maxValue = DisplayedTargetKills;
                progressSlider.value = Mathf.Clamp(currentKills, 0, DisplayedTargetKills);
            }

            if (chargesText != null)
            {
                chargesText.text = string.Format(chargesFormat, currentCharges);
            }

            if (manualTriggerButton != null)
            {
                manualTriggerButton.interactable = currentCharges > 0;
            }
        }

        private void OnChargeProgress(AbilityChargeProgressEvent evt)
        {
            UpdateChargeState(evt.CurrentKills, evt.TargetKills, evt.CurrentCharges);
        }

        private void OnAbilityUsed(AbilityUsedEvent evt)
        {
            DisplayedCharges = evt.RemainingCharges;

            if (chargesText != null)
            {
                chargesText.text = string.Format(chargesFormat, evt.RemainingCharges);
            }

            if (manualTriggerButton != null)
            {
                manualTriggerButton.interactable = evt.RemainingCharges > 0;
            }
        }

        private void BindUiListeners()
        {
            if (manualTriggerButton != null)
            {
                manualTriggerButton.onClick.AddListener(OnManualButtonClicked);

                var trigger = manualTriggerButton.gameObject.GetComponent<EventTrigger>();
                if (trigger == null) trigger = manualTriggerButton.gameObject.AddComponent<EventTrigger>();
                
                trigger.triggers.Clear();
                
                var pd = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                pd.callback.AddListener((data) => OnPointerDown((PointerEventData)data));
                trigger.triggers.Add(pd);

                var d = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
                d.callback.AddListener((data) => OnDrag((PointerEventData)data));
                trigger.triggers.Add(d);

                var pu = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                pu.callback.AddListener((data) => OnPointerUp((PointerEventData)data));
                trigger.triggers.Add(pu);
            }

            if (modeToggleButton != null)
            {
                modeToggleButton.onClick.AddListener(ToggleMode);
            }

            if (modeToggle != null)
            {
                modeToggle.onValueChanged.AddListener(OnModeToggleChanged);
            }
        }

        private void UnbindUiListeners()
        {
            if (manualTriggerButton != null)
            {
                manualTriggerButton.onClick.RemoveListener(OnManualButtonClicked);

                var trigger = manualTriggerButton.gameObject.GetComponent<EventTrigger>();
                if (trigger != null) trigger.triggers.Clear();
            }

            if (modeToggleButton != null)
            {
                modeToggleButton.onClick.RemoveListener(ToggleMode);
            }

            if (modeToggle != null)
            {
                modeToggle.onValueChanged.RemoveListener(OnModeToggleChanged);
            }
        }

        private void CleanupSubscriptions()
        {
            _progressSub?.Dispose();
            _progressSub = null;

            _usedSub?.Dispose();
            _usedSub = null;
        }

        private void Start()
        {
            if (controller == null)
            {
                controller = FindFirstObjectByType<GeneralAbilityController>();
            }

            if (!_isInitialized && controller != null)
            {
                Initialize(controller);
            }
        }

        private void OnDestroy()
        {
            UnbindUiListeners();
            CleanupSubscriptions();
        }

        private bool _isAiming;
        private float _aimStartTime;
        private Vector2 _pointerDownPosition;
        private Vector2 _currentPointerPosition;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (controller == null || controller.CurrentCharges <= 0) return;

            var config = controller.Definition?.Targeting;
            if (config != null && config.Type == Game.Core.Abilities.AbilityTargetingType.GroundTarget)
            {
                _isAiming = true;
                _aimStartTime = Time.unscaledTime;
                _pointerDownPosition = eventData != null ? eventData.position : Vector2.zero;
                _currentPointerPosition = _pointerDownPosition;
                Time.timeScale = 0.3f;
                
                if (aimIndicator != null)
                {
                    aimIndicator.Show(config.Radius);
                }
                UpdateAimVisuals();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isAiming) return;
            _currentPointerPosition = eventData != null ? eventData.position : _pointerDownPosition;
            UpdateAimVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isAiming) return;
            
            _isAiming = false;
            Time.timeScale = 1.0f;
            
            if (aimIndicator != null)
            {
                aimIndicator.Hide();
            }

            if (eventData != null)
            {
                _currentPointerPosition = eventData.position;
            }

            bool isCanceling = Vector2.Distance(_pointerDownPosition, _currentPointerPosition) < 35f;
            
            if (!isCanceling)
            {
                var targetPos = CalculateTargetPosition();
                controller.TriggerAbility(manual: true, targetOverride: targetPos);
            }
        }

        private void Update()
        {
            if (_isAiming)
            {
                if (Time.unscaledTime - _aimStartTime >= 3f)
                {
                    _isAiming = false;
                    Time.timeScale = 1.0f;
                    if (aimIndicator != null)
                    {
                        aimIndicator.Hide();
                    }
                    var targetPos = CalculateTargetPosition();
                    controller.TriggerAbility(manual: true, targetOverride: targetPos);
                }
                else
                {
                    UpdateAimVisuals();
                }
            }
        }

        private void UpdateAimVisuals()
        {
            if (aimIndicator != null && controller != null)
            {
                bool isCanceling = Vector2.Distance(_pointerDownPosition, _currentPointerPosition) < 35f;
                var targetPos = CalculateTargetPosition();
                aimIndicator.UpdateAim(new Vector3(targetPos.X, 0f, targetPos.Z), isCanceling);
            }
        }

        private Game.Core.Abilities.AbilityPosition CalculateTargetPosition()
        {
            if (controller == null || controller.Definition == null) return new Game.Core.Abilities.AbilityPosition(0,0,0);
            
            var config = controller.Definition.Targeting;
            Vector2 delta = _currentPointerPosition - _pointerDownPosition;
            
            float maxDrag = 200f;
            float dragDistance = Mathf.Clamp(delta.magnitude, 0f, maxDrag);
            float normalizedDistance = dragDistance / maxDrag;
            
            float maxRange = config != null ? config.MaxRange : 20f;
            float targetDistance = Mathf.Lerp(0f, maxRange, normalizedDistance);
            
            Vector3 origin = controller.transform.position;
            Vector3 direction = new Vector3(delta.x, 0f, delta.y).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector3.forward;
                targetDistance = maxRange * 0.5f;
            }
            
            Vector3 worldPos = origin + direction * targetDistance;
            return new Game.Core.Abilities.AbilityPosition(worldPos.x, worldPos.y, worldPos.z);
        }
    }
}
