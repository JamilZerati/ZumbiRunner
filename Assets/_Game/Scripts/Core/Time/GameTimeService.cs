using System;

namespace Game.Core.Time
{
    public class GameTimeService : IGameTimeService
    {
        public float GlobalTimeScale { get; set; } = 1f;
        public float AllyTimeScale { get; set; } = 1f;
        public float EnemyTimeScale { get; set; } = 1f;
        public bool IsBulletTimeActive => false;
        public float BulletTimeRemaining => 0f;

        public float GetDeltaTime(TimeTarget target, float unscaledDeltaTime) => throw new NotImplementedException();
        public void ActivateBulletTime(float durationSeconds, float enemyScale = 0.2f, float allyScale = 1.0f) => throw new NotImplementedException();
        public void CancelBulletTime() => throw new NotImplementedException();
        public void Tick(float unscaledDeltaTime) => throw new NotImplementedException();
    }
}
