using System;
using Game.Core.Abilities;

namespace Game.Core.Allies
{
    public sealed class AbilityRunner
    {
        public string AbilityName { get; }
        public float CooldownSeconds { get; }
        public float CooldownRemaining { get; private set; }
        public bool IsReady => CooldownRemaining <= 0f;
        public float FillAmount => CooldownSeconds <= 0f ? 1f : 1f - (CooldownRemaining / CooldownSeconds);

        public AbilityRunner(string abilityName, float cooldownSeconds, float initialCooldown = 0f)
        {
            AbilityName = abilityName ?? string.Empty;
            CooldownSeconds = cooldownSeconds;
            CooldownRemaining = initialCooldown;
        }

        public void Tick(float deltaTime)
        {
            throw new NotImplementedException("AbilityRunner.Tick pendente de implementação no Marco 3.");
        }

        public bool TryTrigger(in AbilityTriggerContext context, IHeroAbilityTriggerPolicy policy, Action onTriggered)
        {
            throw new NotImplementedException("AbilityRunner.TryTrigger pendente de implementação no Marco 3.");
        }

        public void ResetCooldown()
        {
            CooldownRemaining = CooldownSeconds;
        }
    }
}
