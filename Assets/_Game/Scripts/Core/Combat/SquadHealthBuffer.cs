namespace Game.Core
{
    public class SquadHealthBuffer
    {
        public float SoldierMaxHealth => 10f;
        public float CurrentSoldierHealth => 10f;
        public float GeneralMaxHealth => 10f;
        public float CurrentGeneralHealth => 10f;
        public bool IsGeneralAlive => true;

        public SquadHealthBuffer(float soldierMaxHealth = 10f, float generalMaxHealth = 10f) { }

        public int ApplyDamage(float damage, int currentSquadCount, out bool generalDied)
        {
            generalDied = false;
            return 0;
        }

        public void ResetSoldierHealth() { }
        public void ResetGeneralHealth() { }
    }
}
