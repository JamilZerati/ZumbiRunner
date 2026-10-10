using System;

namespace Game.Core.Abilities.Effects
{
    [Serializable]
    public class GrenadeAbilityEffect : IAbilityEffect
    {
        public const float DefaultRadius = 4f;
        public const int DefaultDamage = 150;

        public string Description => "Dispara uma granada na lane causando 150 de dano em área (4 m).";

        public void Execute(AbilityExecutionContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.DamageSink == null)
            {
                throw new ArgumentNullException(nameof(context.DamageSink));
            }

            var center = context.TargetPosition.Z != 0f || context.TargetPosition.X != 0f ? context.TargetPosition : context.OriginPosition;
            context.DamageSink.ApplyAreaDamage(center, DefaultRadius, DefaultDamage, DamageType.Area, this);
        }
    }
}
