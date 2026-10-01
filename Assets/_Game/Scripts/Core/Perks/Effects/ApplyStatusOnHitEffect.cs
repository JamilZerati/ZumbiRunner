using System;
using Game.Core.Status;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class ApplyStatusOnHitEffect : IPerkEffect
    {
        public StatusKind Status;
        public int Stacks;

        public ApplyStatusOnHitEffect()
        {
        }

        public ApplyStatusOnHitEffect(StatusKind status, int stacks)
        {
            Status = status;
            Stacks = stacks;
        }

        public string Description => Stacks > 1 ? $"Munição: {StatusLabel(Status)} x{Stacks}" : $"Munição: {StatusLabel(Status)}";

        public void Apply(PerkContext context)
        {
            context.Loadout?.OnHitStatuses.Add(new StatusApplication(Status, Stacks, context.SourceId ?? (object)this));
        }

        private static string StatusLabel(StatusKind status)
        {
            switch (status)
            {
                case StatusKind.Burn:
                    return "Queimadura";
                case StatusKind.Freeze:
                    return "Congelamento";
                case StatusKind.Frozen:
                    return "Congelado";
                case StatusKind.Slow:
                    return "Lentidão";
                case StatusKind.Shock:
                    return "Choque";
                case StatusKind.Poison:
                    return "Veneno";
                default:
                    return status.ToString();
            }
        }
    }
}
