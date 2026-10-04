using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Abilities;
using Game.Core.Abilities.Policies;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Abilities
{
    public class GeneralAbilityController : MonoBehaviour, IAbilityDamageSink
    {
        [SerializeField] private GeneralAbilityDefinition definition;
        private GeneralAbilityDefinition _definition;
        private IEventBus _eventBus;
        private IHeroAbilityTriggerPolicy _triggerPolicy;
        private AbilityChargeTracker _tracker;
        private Transform _generalTransform;
        private IDisposable _enemyKilledSub;
        private Func<bool> _targetsDetector;

        public GeneralAbilityDefinition Definition => _definition != null ? _definition : definition;
        public AbilityChargeTracker Tracker => _tracker;
        public IHeroAbilityTriggerPolicy TriggerPolicy => _triggerPolicy;
        public int CurrentCharges => _tracker != null ? _tracker.CurrentCharges : 0;
        public int CurrentKills => _tracker != null ? _tracker.CurrentKills : 0;
        public int KillsPerCharge => _tracker != null ? _tracker.KillsPerCharge : (Definition != null ? Definition.ChargeKills : 25);

        public void Initialize(
            GeneralAbilityDefinition definition,
            IEventBus eventBus,
            IHeroAbilityTriggerPolicy triggerPolicy = null,
            RunConfig runConfig = null,
            Transform generalTransform = null)
        {
            _enemyKilledSub?.Dispose();
            _enemyKilledSub = null;

            _definition = definition != null ? definition : this.definition;
            this.definition = _definition;
            _eventBus = eventBus;
            _triggerPolicy = triggerPolicy ?? new AutoHeroAbilityTriggerPolicy();
            _generalTransform = generalTransform != null ? generalTransform : transform;

            int killsPerCharge = definition != null ? definition.ChargeKills : 25;
            int initialCharges = runConfig != null ? runConfig.BonusAbilityCharges : 0;
            _tracker = new AbilityChargeTracker(killsPerCharge, initialCharges);

            if (_eventBus != null)
            {
                _enemyKilledSub = _eventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
            }

            PublishChargeProgress();
        }

        public void SetTriggerPolicy(IHeroAbilityTriggerPolicy policy)
        {
            _triggerPolicy = policy ?? new AutoHeroAbilityTriggerPolicy();
        }

        public void SetTargetsDetector(Func<bool> detector)
        {
            _targetsDetector = detector;
        }

        public bool HasTargetsInLane()
        {
            if (_targetsDetector != null)
            {
                return _targetsDetector();
            }

            Vector3 origin = _generalTransform != null ? _generalTransform.position : transform.position;
            Collider[] hits = Physics.OverlapSphere(origin, 20f, CollisionLayers.EnemyMask);
            return hits != null && hits.Length > 0;
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (_tracker == null)
            {
                return;
            }

            _tracker.RegisterKill(evt.ByAbility);
            PublishChargeProgress();

            if (_triggerPolicy != null && _triggerPolicy.Mode == HeroAbilityTriggerMode.Auto)
            {
                bool hasTargets = HasTargetsInLane();
                var context = new AbilityTriggerContext(_tracker.CurrentCharges, hasTargets, manualTriggerRequested: false);
                if (_triggerPolicy.ShouldTrigger(context))
                {
                    TriggerAbility(manual: false);
                }
            }
        }

        public bool TriggerAbility(bool manual = false)
        {
            if (_definition == null || _definition.Effect == null || _tracker == null)
            {
                return false;
            }

            bool hasTargets = HasTargetsInLane();
            var triggerContext = new AbilityTriggerContext(_tracker.CurrentCharges, hasTargets, manualTriggerRequested: manual);

            if (_triggerPolicy != null && !_triggerPolicy.ShouldTrigger(triggerContext))
            {
                return false;
            }

            if (!_tracker.TryConsumeCharge())
            {
                return false;
            }

            Vector3 generalPos = _generalTransform != null ? _generalTransform.position : transform.position;
            int currentLane = 0;
            if (_generalTransform != null && _generalTransform.TryGetComponent<LaneMover>(out var mover))
            {
                currentLane = mover.CurrentLane;
            }
            else if (TryGetComponent<LaneMover>(out var localMover))
            {
                currentLane = localMover.CurrentLane;
            }

            var executionContext = new AbilityExecutionContext
            {
                OriginPosition = new AbilityPosition(generalPos.x, generalPos.y, generalPos.z),
                TargetLane = currentLane,
                DamageSink = this,
                EventBus = _eventBus
            };

            _definition.Effect.Execute(executionContext);
            _eventBus?.Publish(new AbilityUsedEvent(_definition.Id, _tracker.CurrentCharges));
            PublishChargeProgress();

            return true;
        }

        public int ApplyAreaDamage(AbilityPosition center, float radius, int damage, DamageType type = DamageType.Area, object source = null)
        {
            Vector3 centerVec = new Vector3(center.X, center.Y, center.Z);
            Collider[] colliders = Physics.OverlapSphere(centerVec, radius, CollisionLayers.EnemyMask);

            int totalDealt = 0;
            var processed = new HashSet<EnemyController>();

            foreach (var col in colliders)
            {
                var enemy = col.GetComponent<EnemyController>()
                    ?? col.GetComponentInParent<EnemyController>()
                    ?? (col.attachedRigidbody != null ? col.attachedRigidbody.GetComponent<EnemyController>() : null);

                if (enemy != null && enemy.IsAlive && processed.Add(enemy))
                {
                    totalDealt += enemy.ReceiveHit(new DamageInfo(damage, type, source ?? this), null);
                }
            }

            return totalDealt;
        }

        private void PublishChargeProgress()
        {
            if (_eventBus != null && _definition != null && _tracker != null)
            {
                _eventBus.Publish(new AbilityChargeProgressEvent(
                    _definition.Id,
                    _tracker.CurrentKills,
                    _tracker.KillsPerCharge,
                    _tracker.CurrentCharges));
            }
        }

        private void Awake()
        {
            if (_tracker == null && definition != null)
            {
                Initialize(definition, _eventBus);
            }
        }

        private void OnDestroy()
        {
            _enemyKilledSub?.Dispose();
            _enemyKilledSub = null;
        }
    }
}
