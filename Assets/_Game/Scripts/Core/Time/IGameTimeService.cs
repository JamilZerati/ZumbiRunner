namespace Game.Core.Time
{
    public interface IGameTimeService
    {
        float GlobalTimeScale { get; set; }
        float AllyTimeScale { get; set; }
        float EnemyTimeScale { get; set; }
        bool IsBulletTimeActive { get; }
        float BulletTimeRemaining { get; }

        float GetDeltaTime(TimeTarget target, float unscaledDeltaTime);
        void ActivateBulletTime(float durationSeconds, float enemyScale = 0.2f, float allyScale = 1.0f);
        void CancelBulletTime();
        void Tick(float unscaledDeltaTime);
    }
}
