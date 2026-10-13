using System;
using Game.Core;
using Game.Core.Events;
using Game.Core.Holdout;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay
{
    public class HoldoutController : MonoBehaviour
    {
        [SerializeField] private HoldoutDefinition definition;
        [SerializeField] private Barricade barricade;
        [SerializeField] private TrackScroller trackScroller;

        private HoldoutRuntimeState _runtimeState;
        private ISquad _squad;
        private LaneMover _laneMover;
        private IEventBus _eventBus;
        private IDisposable _laneChangedSub;
        private bool _isActive;
        private bool _isCompleted;
        private float _waveTimer;
        private int _waveIndex;

        public HoldoutDefinition Definition => definition;
        public Barricade Barricade => barricade;
        public TrackScroller TrackScroller => trackScroller;
        public ISquad Squad => _squad;
        public LaneMover LaneMover => _laneMover;
        public IEventBus EventBus => _eventBus;
        public IHoldoutState State => _runtimeState ?? barricade?.State;
        public bool IsActive => _isActive;
        public bool IsCompleted => _isCompleted;
        public float TimeRemaining => State != null ? State.TimeRemaining : 0f;
        public int CurrentWave => _waveIndex;

        public event Action<HoldoutController> OnHoldoutStarted;
        public event Action<HoldoutController, int> OnWaveTriggered;
        public event Action<HoldoutController, bool> OnHoldoutCompleted;

        public void Initialize(
            HoldoutDefinition def,
            Barricade bar,
            TrackScroller scroller = null,
            ISquad squad = null,
            LaneMover laneMover = null,
            IEventBus eventBus = null,
            HoldoutSegmentSpec spec = null)
        {
            definition = def;
            barricade = bar;
            trackScroller = scroller;
            _squad = squad;
            _laneMover = laneMover;
            _eventBus = eventBus;
            _isActive = false;
            _isCompleted = false;
            _waveTimer = 0f;
            _waveIndex = 0;

            string id = spec != null && !string.IsNullOrEmpty(spec.HoldoutId)
                ? spec.HoldoutId
                : (def != null ? def.Id : "holdout_standard");

            int maxHp = spec != null && spec.CustomMaxHp > 0
                ? spec.CustomMaxHp
                : (def != null ? def.MaxHp : 150);

            int survivors = spec != null && spec.CustomSurvivors > 0
                ? spec.CustomSurvivors
                : (def != null ? def.TotalSurvivors : 4);

            float duration = spec != null && spec.CustomDuration > 0f
                ? spec.CustomDuration
                : (def != null ? def.DurationSeconds : 12f);

            _runtimeState = new HoldoutRuntimeState(id, maxHp, survivors, duration);

            int initialLane = _laneMover != null ? _laneMover.CurrentLane : (barricade != null ? barricade.GuardedLane : 1);
            if (barricade != null)
            {
                barricade.Initialize(def, _runtimeState, initialLane, eventBus);
                barricade.OnDestroyed -= HandleBarricadeDestroyed;
                barricade.OnDestroyed += HandleBarricadeDestroyed;
            }

            _laneChangedSub?.Dispose();
            if (_eventBus != null)
            {
                _laneChangedSub = _eventBus.Subscribe<LaneChangedEvent>(OnLaneChanged);
            }
        }

        public void StartHoldout()
        {
            if (_isActive || _isCompleted)
            {
                return;
            }

            _isActive = true;
            _isCompleted = false;
            _waveTimer = 0f;
            _waveIndex = 0;

            if (trackScroller != null)
            {
                trackScroller.IsPaused = true;
            }

            if (_runtimeState != null)
            {
                _runtimeState.StartDefense();
            }

            if (_laneMover != null && barricade != null)
            {
                barricade.SetGuardedLane(_laneMover.CurrentLane);
            }

            if (_eventBus != null && _runtimeState != null)
            {
                _eventBus.Publish(new HoldoutStartedEvent(
                    _runtimeState.HoldoutId,
                    _runtimeState.MaxHp,
                    _runtimeState.TotalSurvivors,
                    _runtimeState.DurationSeconds));
            }

            OnHoldoutStarted?.Invoke(this);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive || _isCompleted || deltaTime <= 0f)
            {
                return;
            }

            if (_laneMover != null && barricade != null)
            {
                barricade.SetGuardedLane(_laneMover.CurrentLane);
            }

            if (!Application.isPlaying && barricade != null)
            {
                barricade.TickDamage(deltaTime);
            }

            if (_runtimeState != null)
            {
                _runtimeState.Tick(deltaTime);

                float waveInterval = definition != null && definition.WaveIntervalSeconds > 0f
                    ? definition.WaveIntervalSeconds
                    : 3f;

                _waveTimer += deltaTime;
                if (_waveTimer >= waveInterval)
                {
                    _waveTimer -= waveInterval;
                    _waveIndex++;
                    OnWaveTriggered?.Invoke(this, _waveIndex);
                }

                if (_runtimeState.IsBarricadeDestroyed || (barricade != null && barricade.IsDestroyed))
                {
                    HandleBarricadeDestroyed(barricade);
                }
                else if (_runtimeState.IsVictory || _runtimeState.TimeRemaining <= 0f)
                {
                    HandleHoldoutVictory();
                }
            }
        }

        private void HandleHoldoutVictory()
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            _isActive = false;

            if (trackScroller != null)
            {
                trackScroller.IsPaused = false;
            }

            if (_runtimeState != null && !_runtimeState.IsVictory)
            {
                _runtimeState.ConcludeHoldout(timeoutOrCleared: true);
            }

            int saved = _runtimeState != null ? _runtimeState.SurvivorsSaved : 0;
            int remainingHp = _runtimeState != null ? _runtimeState.CurrentHp : 0;
            string id = _runtimeState != null ? _runtimeState.HoldoutId : string.Empty;
            int lane = barricade != null ? barricade.GuardedLane : 0;

            if (saved > 0 && _squad != null)
            {
                _squad.Add(saved);
            }

            if (_eventBus != null)
            {
                _eventBus.Publish(new HoldoutEndedEvent(id, saved, remainingHp, barricadeHeld: true));
                if (saved > 0)
                {
                    _eventBus.Publish(new SurvivorsRescuedEvent(saved, lane, id));
                }
            }

            OnHoldoutCompleted?.Invoke(this, true);
        }

        private void HandleBarricadeDestroyed(Barricade bar)
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            _isActive = false;

            if (trackScroller != null)
            {
                trackScroller.IsPaused = false;
            }

            if (_runtimeState != null && !_runtimeState.IsBarricadeDestroyed)
            {
                _runtimeState.ConcludeHoldout(timeoutOrCleared: false);
            }

            int totalSurvivors = _runtimeState != null ? _runtimeState.TotalSurvivors : 0;
            string id = _runtimeState != null ? _runtimeState.HoldoutId : string.Empty;
            int lane = barricade != null ? barricade.GuardedLane : 0;

            if (_eventBus != null)
            {
                _eventBus.Publish(new HoldoutEndedEvent(id, 0, 0, barricadeHeld: false));
                if (totalSurvivors > 0)
                {
                    _eventBus.Publish(new SurvivorsLostEvent(totalSurvivors, lane, id));
                }
            }

            OnHoldoutCompleted?.Invoke(this, false);
        }

        private void OnLaneChanged(LaneChangedEvent evt)
        {
            if (barricade != null)
            {
                barricade.SetGuardedLane(evt.NewLane);
            }
        }

        private void OnDestroy()
        {
            _laneChangedSub?.Dispose();
            _laneChangedSub = null;

            if (barricade != null)
            {
                barricade.OnDestroyed -= HandleBarricadeDestroyed;
            }
        }
    }
}
