using System;
using Game.Core.Events;

namespace Game.Core.Time
{
    public class GameTimeService : IGameTimeService
    {
        private readonly IEventBus _eventBus;
        private float _allyTimeScale = 1f;
        private float _enemyTimeScale = 1f;
        private float _baseAllyTimeScale = 1f;
        private float _baseEnemyTimeScale = 1f;
        private float _bulletTimeRemaining;

        public float GlobalTimeScale { get; set; } = 1f;

        public float AllyTimeScale
        {
            get => _allyTimeScale;
            set
            {
                _allyTimeScale = value;
                if (!IsBulletTimeActive)
                {
                    _baseAllyTimeScale = value;
                }
            }
        }

        public float EnemyTimeScale
        {
            get => _enemyTimeScale;
            set
            {
                _enemyTimeScale = value;
                if (!IsBulletTimeActive)
                {
                    _baseEnemyTimeScale = value;
                }
            }
        }

        public bool IsBulletTimeActive => _bulletTimeRemaining > 0f;
        public float BulletTimeRemaining => _bulletTimeRemaining;

        public GameTimeService(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
        }

        public float GetDeltaTime(TimeTarget target, float unscaledDeltaTime)
        {
            switch (target)
            {
                case TimeTarget.Global:
                    return unscaledDeltaTime * GlobalTimeScale;
                case TimeTarget.Ally:
                    return unscaledDeltaTime * GlobalTimeScale * AllyTimeScale;
                case TimeTarget.Enemy:
                    return unscaledDeltaTime * GlobalTimeScale * EnemyTimeScale;
                default:
                    return unscaledDeltaTime * GlobalTimeScale;
            }
        }

        public void ActivateBulletTime(float durationSeconds, float enemyScale = 0.2f, float allyScale = 1.0f)
        {
            if (durationSeconds <= 0f)
            {
                CancelBulletTime();
                return;
            }

            if (!IsBulletTimeActive)
            {
                _baseAllyTimeScale = _allyTimeScale;
                _baseEnemyTimeScale = _enemyTimeScale;
            }

            _bulletTimeRemaining = durationSeconds;
            _enemyTimeScale = enemyScale;
            _allyTimeScale = allyScale;

            _eventBus?.Publish(new BulletTimeStartedEvent(durationSeconds, enemyScale, allyScale));
        }

        public void CancelBulletTime()
        {
            if (!IsBulletTimeActive)
            {
                return;
            }

            _bulletTimeRemaining = 0f;
            _enemyTimeScale = _baseEnemyTimeScale;
            _allyTimeScale = _baseAllyTimeScale;
            _eventBus?.Publish(new BulletTimeEndedEvent());
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (!IsBulletTimeActive || unscaledDeltaTime <= 0f)
            {
                return;
            }

            _bulletTimeRemaining -= unscaledDeltaTime;
            if (_bulletTimeRemaining <= 0f)
            {
                _bulletTimeRemaining = 0f;
                _enemyTimeScale = _baseEnemyTimeScale;
                _allyTimeScale = _baseAllyTimeScale;
                _eventBus?.Publish(new BulletTimeEndedEvent());
            }
        }
    }
}
