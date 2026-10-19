namespace Game.Core.Events
{
    public class BossSpawnedEvent
    {
        public string BossId { get; }
        public float Distance { get; }
        public int MaxHp { get; }

        public BossSpawnedEvent(string bossId, float distance, int maxHp)
        {
            BossId = bossId;
            Distance = distance;
            MaxHp = maxHp;
        }
    }
}
