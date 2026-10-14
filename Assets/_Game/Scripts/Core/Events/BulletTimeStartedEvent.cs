namespace Game.Core.Events
{
    public readonly struct BulletTimeStartedEvent
    {
        public float DurationSeconds { get; }
        public float EnemyScale { get; }
        public float AllyScale { get; }

        public BulletTimeStartedEvent(float durationSeconds, float enemyScale, float allyScale)
        {
            DurationSeconds = durationSeconds;
            EnemyScale = enemyScale;
            AllyScale = allyScale;
        }
    }
}
