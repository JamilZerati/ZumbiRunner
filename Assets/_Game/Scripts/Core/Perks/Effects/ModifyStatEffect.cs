using System;
using System.Globalization;
using Game.Core.Stats;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class ModifyStatEffect : IPerkEffect
    {
        public StatId Stat;
        public ModifierKind Kind;
        public float Value;

        public ModifyStatEffect()
        {
        }

        public ModifyStatEffect(StatId stat, ModifierKind kind, float value)
        {
            Stat = stat;
            Kind = kind;
            Value = value;
        }

        public string Description => $"{FormatAmount()} {StatLabel(Stat)}";

        public void Apply(PerkContext context)
        {
            context.Loadout?.Stats.AddModifier(new StatModifier(Stat, Kind, Value, context.SourceId ?? (object)this));
        }

        private string FormatAmount()
        {
            switch (Kind)
            {
                case ModifierKind.PercentAdd:
                    return $"{Signed(Value * 100f)}%";
                case ModifierKind.PercentMultiply:
                    return $"x{Format(1f + Value)}";
                default:
                    return Signed(Value);
            }
        }

        private static string Signed(float amount) => amount >= 0f ? $"+{Format(amount)}" : Format(amount);

        private static string Format(float amount) => amount.ToString("0.##", CultureInfo.InvariantCulture);

        private static string StatLabel(StatId stat)
        {
            switch (stat)
            {
                case StatId.Damage:
                    return "Dano";
                case StatId.FireRate:
                    return "Cadência";
                case StatId.ProjectileSpeed:
                    return "Velocidade";
                case StatId.Range:
                    return "Alcance";
                case StatId.ProjectileCount:
                    return "Projéteis";
                default:
                    return stat.ToString();
            }
        }
    }
}
