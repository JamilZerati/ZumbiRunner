using System;

namespace Game.Core.Abilities.Effects
{
    public class GrenadeAbilityEffect : IAbilityEffect
    {
        public const float DefaultRadius = 4f;
        public const int DefaultDamage = 150;

        public string Description => throw new NotImplementedException();

        public void Execute(AbilityExecutionContext context)
        {
            throw new NotImplementedException();
        }
    }
}
