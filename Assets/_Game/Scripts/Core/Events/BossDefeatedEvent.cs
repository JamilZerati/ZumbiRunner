namespace Game.Core.Events
{
    public class BossDefeatedEvent
    {
        public string BossId { get; }
        public float Distance { get; }

        public BossDefeatedEvent(string bossId, float distance)
        {
            BossId = bossId;
            Distance = distance;
        }
    }
}
