namespace Game.Core.Abilities
{
    public interface IAbilityDamageSink
    {
        int ApplyAreaDamage(AbilityPosition center, float radius, int damage, DamageType type = DamageType.Area, object source = null);
    }
}
