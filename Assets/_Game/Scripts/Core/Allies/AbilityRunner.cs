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
        public float FillAmount
        {
            get
            {
                if (CooldownSeconds <= 0f) return 1f;
                float clamped = CooldownRemaining < 0f ? 0f : (CooldownRemaining > CooldownSeconds ? CooldownSeconds : CooldownRemaining);
                return 1f - (clamped / CooldownSeconds);
            }
        }

        public AbilityRunner(string abilityName, float cooldownSeconds, float initialCooldown = 0f)
        {
            AbilityName = abilityName ?? string.Empty;
            CooldownSeconds = Math.Max(0f, cooldownSeconds);
            CooldownRemaining = Math.Max(0f, initialCooldown);
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || CooldownRemaining <= 0f)
            {
                return;
            }

            CooldownRemaining = Math.Max(0f, CooldownRemaining - deltaTime);
        }

        public bool TryTrigger(in AbilityTriggerContext context, IHeroAbilityTriggerPolicy policy, Action onTriggered)
        {
            if (!IsReady)
            {
                return false;
            }

            if (policy == null || !policy.ShouldTrigger(context))
            {
                return false;
            }

            onTriggered?.Invoke();
            ResetCooldown();
            return true;
        }

        public void ResetCooldown()
        {
            CooldownRemaining = CooldownSeconds;
        }
    }
}
