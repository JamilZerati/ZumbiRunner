using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Core.Status;

namespace Game.Core.Diagnostics
{
    public static class DebugOverlayFormatter
    {
        private static readonly StatId[] AllStats = (StatId[])Enum.GetValues(typeof(StatId));

        public static string Format(in DebugOverlayData data)
        {
            var sb = new StringBuilder(512);

            string weaponName = string.IsNullOrEmpty(data.WeaponId) ? "(padrao)" : data.WeaponId;
            int targetKills = data.GrenadeTargetKills > 0 ? data.GrenadeTargetKills : 25;

            sb.Append("[DEBUG OVERLAY]\n");
            sb.AppendFormat(CultureInfo.InvariantCulture, "Tropa: {0} | Distância: {1:F1}m / {2:F1}m\n",
                data.SquadCount, data.DistanceTraveled, data.VictoryDistance);

            sb.AppendFormat(CultureInfo.InvariantCulture, "Arma: {0} | Dano: {1} | Cadência: {2:F1} | Alcance: {3:F1} | Projéteis: {4}\n",
                weaponName, data.WeaponStats.Damage, data.WeaponStats.FireRate, data.WeaponStats.Range, data.WeaponStats.ProjectileCount);

            AppendActiveStatuses(sb, data.ActiveStatuses);
            AppendModifiers(sb, data.Stats);
            AppendRecentGates(sb, data.RecentGates);
            AppendRecentSynergies(sb, data.RecentSynergies);

            sb.AppendFormat(CultureInfo.InvariantCulture, "Granada: {0}/{1} abates | Cargas: {2} | Modo: {3} | Usos: {4}",
                data.GrenadeKills, targetKills, data.GrenadeCharges, data.GrenadeMode, data.GrenadeUses);

            return sb.ToString();
        }

        private static void AppendActiveStatuses(StringBuilder sb, IReadOnlyList<StatusApplication> statuses)
        {
            sb.Append("Elementos: ");
            if (statuses == null || statuses.Count == 0)
            {
                sb.Append("Nenhum\n");
                return;
            }

            for (int i = 0; i < statuses.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} (x{1})", statuses[i].Kind, statuses[i].Stacks);
            }
            sb.Append('\n');
        }

        private static void AppendModifiers(StringBuilder sb, StatCollection stats)
        {
            if (stats == null)
            {
                sb.Append("Modificadores: Nenhum\n");
                return;
            }

            var grouped = new Dictionary<string, List<string>>();
            int totalModifiers = 0;

            for (int i = 0; i < AllStats.Length; i++)
            {
                var stat = AllStats[i];
                var mods = stats.GetModifiers(stat);
                if (mods == null || mods.Count == 0) continue;

                for (int m = 0; m < mods.Count; m++)
                {
                    var mod = mods[m];
                    string sourceName = mod.Source?.ToString() ?? "Desconhecido";
                    if (!grouped.TryGetValue(sourceName, out var list))
                    {
                        list = new List<string>();
                        grouped[sourceName] = list;
                    }

                    string valueStr = FormatModifierValue(mod);
                    list.Add(string.Format(CultureInfo.InvariantCulture, "{0} {1}", mod.Stat, valueStr));
                    totalModifiers++;
                }
            }

            if (totalModifiers == 0)
            {
                sb.Append("Modificadores: Nenhum\n");
                return;
            }

            sb.Append("Modificadores:\n");
            foreach (var kvp in grouped)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "  [{0}]: {1}\n", kvp.Key, string.Join(", ", kvp.Value));
            }
        }

        private static string FormatModifierValue(in StatModifier mod)
        {
            switch (mod.Kind)
            {
                case ModifierKind.Flat:
                    return mod.Value >= 0 ? "+" + mod.Value.ToString("0.##", CultureInfo.InvariantCulture) : mod.Value.ToString("0.##", CultureInfo.InvariantCulture);
                case ModifierKind.PercentAdd:
                    float pctAdd = mod.Value * 100f;
                    return (pctAdd >= 0 ? "+" : "") + pctAdd.ToString("0.##", CultureInfo.InvariantCulture) + "%";
                case ModifierKind.PercentMultiply:
                    float pctMul = mod.Value * 100f;
                    return "x(1" + (pctMul >= 0 ? "+" : "") + pctMul.ToString("0.##", CultureInfo.InvariantCulture) + "%)";
                default:
                    return mod.Value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static void AppendRecentGates(StringBuilder sb, IReadOnlyList<GateTriggeredEvent> gates)
        {
            if (gates == null || gates.Count == 0)
            {
                sb.Append("Portões: Nenhum\n");
                return;
            }

            sb.Append("Portões: ");
            for (int i = 0; i < gates.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.AppendFormat(CultureInfo.InvariantCulture, "L{0}:{1}", gates[i].LaneIndex, gates[i].PerkId);
            }
            sb.Append('\n');
        }

        private static void AppendRecentSynergies(StringBuilder sb, IReadOnlyList<SynergyTriggeredEvent> synergies)
        {
            if (synergies == null || synergies.Count == 0)
            {
                sb.Append("Sinergias: Nenhuma\n");
                return;
            }

            sb.Append("Sinergias: ");
            for (int i = 0; i < synergies.Count; i++)
            {
                if (i > 0) sb.Append(" | ");
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ({1} -> {2})",
                    synergies[i].InteractionId, synergies[i].HitDamage, synergies[i].FinalDamage);
            }
            sb.Append('\n');
        }
    }
}
