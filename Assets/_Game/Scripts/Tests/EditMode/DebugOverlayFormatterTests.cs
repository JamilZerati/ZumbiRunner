using System.Collections.Generic;
using Game.Core.Abilities;
using Game.Core.Diagnostics;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Core.Status;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class DebugOverlayFormatterTests
    {
        [Test]
        public void Format_ComDadosCompletos_FormataTodasAsSecoesRequeridas()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 15f);
            stats.SetBase(StatId.FireRate, 3.2f);
            stats.SetBase(StatId.Range, 25f);
            stats.SetBase(StatId.ProjectileCount, 2f);

            var perkSource = "Perk_FireRatePlus";
            stats.AddModifier(new StatModifier(StatId.FireRate, ModifierKind.PercentAdd, 0.5f, perkSource));

            var activeStatuses = new List<StatusApplication>
            {
                new StatusApplication(StatusKind.Burn, 1, "Perk_Fire"),
                new StatusApplication(StatusKind.Freeze, 2, "Perk_Freeze")
            };

            var recentGates = new List<GateTriggeredEvent>
            {
                new GateTriggeredEvent(0, "perk_fire_rate"),
                new GateTriggeredEvent(1, "perk_damage")
            };

            var recentSynergies = new List<SynergyTriggeredEvent>
            {
                new SynergyTriggeredEvent("shatter", null, 12, 24)
            };

            var data = new DebugOverlayData
            {
                SquadCount = 12,
                DistanceTraveled = 45.6f,
                VictoryDistance = 120.0f,
                WeaponId = "shotgun",
                WeaponStats = WeaponStats.Resolve(stats, 0f),
                ActiveStatuses = activeStatuses,
                Stats = stats,
                RecentGates = recentGates,
                RecentSynergies = recentSynergies,
                GrenadeKills = 18,
                GrenadeTargetKills = 25,
                GrenadeCharges = 2,
                GrenadeMode = HeroAbilityTriggerMode.Auto,
                GrenadeUses = 3
            };

            string output = DebugOverlayFormatter.Format(data);

            Assert.IsNotEmpty(output);
            StringAssert.Contains("Tropa: 12", output);
            StringAssert.Contains("45.6", output);
            StringAssert.Contains("120.0", output);
            StringAssert.Contains("shotgun", output);
            StringAssert.Contains("Dano: 15", output);
            StringAssert.Contains("Cadência: 4.8", output);
            StringAssert.Contains("Alcance: 25.0", output);
            StringAssert.Contains("Projéteis: 2", output);
            StringAssert.Contains("Burn (x1)", output);
            StringAssert.Contains("Freeze (x2)", output);
            StringAssert.Contains("Perk_FireRatePlus", output);
            StringAssert.Contains("perk_fire_rate", output);
            StringAssert.Contains("shatter", output);
            StringAssert.Contains("12 -> 24", output);
            StringAssert.Contains("18/25", output);
            StringAssert.Contains("Cargas: 2", output);
            StringAssert.Contains("Modo: Auto", output);
            StringAssert.Contains("Usos: 3", output);
        }

        [Test]
        public void Format_ComDadosVazios_NaoLancaExcecaoEExibeFallbacksLegiveis()
        {
            var data = new DebugOverlayData();

            string output = null;
            Assert.DoesNotThrow(() => output = DebugOverlayFormatter.Format(data));

            Assert.IsNotNull(output);
            StringAssert.Contains("Tropa: 0", output);
            StringAssert.Contains("Arma: (padrao)", output);
            StringAssert.Contains("Elementos: Nenhum", output);
            StringAssert.Contains("Modificadores: Nenhum", output);
            StringAssert.Contains("Portões: Nenhum", output);
            StringAssert.Contains("Sinergias: Nenhuma", output);
            StringAssert.Contains("Granada: 0/25", output);
        }

        [Test]
        public void Format_ModificadoresAgrupadosPorSource_ExibeMultiplosModifiersSobMesmaFonte()
        {
            var stats = new StatCollection();
            stats.SetBase(StatId.Damage, 10f);
            stats.SetBase(StatId.Range, 20f);

            var sourceA = "FonteA";
            var sourceB = "FonteB";

            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.Flat, 5f, sourceA));
            stats.AddModifier(new StatModifier(StatId.Range, ModifierKind.PercentAdd, 0.2f, sourceA));
            stats.AddModifier(new StatModifier(StatId.Damage, ModifierKind.PercentMultiply, 0.1f, sourceB));

            var data = new DebugOverlayData
            {
                Stats = stats
            };

            string output = DebugOverlayFormatter.Format(data);

            StringAssert.Contains("FonteA", output);
            StringAssert.Contains("FonteB", output);
            StringAssert.Contains("Damage +5", output);
            StringAssert.Contains("Range +20%", output);
        }
    }
}
