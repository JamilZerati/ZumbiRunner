using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Game.Data;
using Game.Core.Gates;

namespace Game.Editor.Tools
{
    public enum BotType { Guloso, Fujao }

    [Serializable]
    public class SimulationRunResult
    {
        public int runIndex, finalSquad, multiplierReached, kills, rescued;
        public string botType;
        public bool victory;
        public float distanceReached;

        public int RunIndex => runIndex;
        public BotType Bot => (BotType)Enum.Parse(typeof(BotType), botType);
        public bool Victory => victory;
        public float DistanceReached => distanceReached;
        public int FinalSquad => finalSquad;
        public int MultiplierReached => multiplierReached;
        public int Kills => kills;
        public int Rescued => rescued;
    }

    [Serializable]
    public class SimulationSummary
    {
        public string botType;
        public int totalRuns, victories, totalRescued;
        public float winRate, averageDistance, averageFinalSquad, averageMultiplier, averageRescued;
        public List<SimulationRunResult> runs = new List<SimulationRunResult>();

        public BotType Bot => (BotType)Enum.Parse(typeof(BotType), botType);
        public int TotalRuns => totalRuns;
        public int Victories => victories;
        public float WinRate => winRate;
        public float AverageDistance => averageDistance;
        public float AverageFinalSquad => averageFinalSquad;
        public float AverageMultiplier => averageMultiplier;
        public float AverageRescued => averageRescued;
        public int TotalRescued => totalRescued;
        public IReadOnlyList<SimulationRunResult> Runs => runs;
    }

    [Serializable]
    public class SimulationReport
    {
        public string levelId;
        public int seed, runsPerBot;
        public List<SimulationSummary> summaries = new List<SimulationSummary>();

        public string LevelId => levelId;
        public int Seed => seed;
        public int RunsPerBot => runsPerBot;
        public IReadOnlyList<SimulationSummary> Summaries => summaries;
        public SimulationSummary GetSummary(BotType type) => summaries.Find(s => s.botType == type.ToString());
    }

    public static class SimulateCommand
    {
        [MenuItem("Horde Runner/Content/Simulate Level 01")]
        public static void SimulateMenuItem() => RunAll("level_01", 42, 10);
        public static void Run() => ExecuteFromCommandLine();

        public static LevelDefinition LoadLevel(string levelId)
        {
            levelId = string.IsNullOrEmpty(levelId) ? "level_01" : levelId;
            var level = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/_Game/Data/Levels/{levelId}.asset");
            if (level != null) return level;
            string jsonPath = Path.Combine("Content", "Source", "Levels", $"{levelId}.json");
            if (File.Exists(jsonPath))
            {
                var def = ScriptableObject.CreateInstance<LevelDefinition>();
                JsonUtility.FromJsonOverwrite(File.ReadAllText(jsonPath), def);
                return def;
            }
            return null;
        }

        public static SimulationReport RunAll(string levelId = "level_01", int seed = 42, int runs = 10, string outputPath = null)
        {
            var level = LoadLevel(levelId);
            if (level == null) throw new FileNotFoundException($"Level '{levelId}' could not be loaded.");
            var report = new SimulationReport { levelId = levelId, seed = seed, runsPerBot = runs };
            report.summaries.Add(RunSimulation(level, BotType.Guloso, runs, seed));
            report.summaries.Add(RunSimulation(level, BotType.Fujao, runs, seed));

            string finalOutput = !string.IsNullOrEmpty(outputPath) ? outputPath : Path.Combine(Directory.GetCurrentDirectory(), ".artifacts", "simulation.json");
            string dir = Path.GetDirectoryName(finalOutput);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(finalOutput, JsonUtility.ToJson(report, true));
            return report;
        }

        public static SimulationSummary RunSimulation(string levelId, BotType botType, int runs = 10, int seed = 42)
        {
            var level = LoadLevel(levelId);
            if (level == null) throw new FileNotFoundException($"Level '{levelId}' could not be loaded.");
            return RunSimulation(level, botType, runs, seed);
        }

        public static SimulationSummary RunSimulation(LevelDefinition level, BotType botType, int runs = 10, int seed = 42)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            var summary = new SimulationSummary { botType = botType.ToString(), totalRuns = runs };
            float totalDistance = 0f;
            int totalSquad = 0, totalMultiplier = 0, totalRescued = 0;

            for (int i = 0; i < runs; i++)
            {
                var r = SimulateSingleRun(level, botType, seed, i);
                summary.runs.Add(r);
                if (r.victory) summary.victories++;
                totalDistance += r.distanceReached;
                totalSquad += r.finalSquad;
                totalMultiplier += r.multiplierReached;
                totalRescued += r.rescued;
            }

            summary.totalRescued = totalRescued;
            summary.winRate = runs > 0 ? (float)summary.victories / runs : 0f;
            summary.averageDistance = runs > 0 ? totalDistance / runs : 0f;
            summary.averageFinalSquad = runs > 0 ? (float)totalSquad / runs : 0f;
            summary.averageMultiplier = runs > 0 ? (float)totalMultiplier / runs : 0f;
            summary.averageRescued = runs > 0 ? (float)totalRescued / runs : 0f;
            return summary;
        }

        private readonly struct ArchetypeStats
        {
            public readonly int Hp;
            public readonly float Speed;
            public readonly int ContactDamage;

            public ArchetypeStats(int hp, float speed, int contactDamage)
            {
                Hp = hp;
                Speed = speed;
                ContactDamage = contactDamage;
            }
        }

        private static readonly Dictionary<string, ArchetypeStats> Archetypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { "walker", new ArchetypeStats(20, 2.0f, 1) },
            { "runner", new ArchetypeStats(12, 5.0f, 1) },
            { "brute", new ArchetypeStats(300, 1.2f, 5) },
            { "exploder", new ArchetypeStats(30, 2.5f, 3) },
            { "spitter", new ArchetypeStats(40, 1.0f, 2) },
            { "shielded", new ArchetypeStats(80, 1.5f, 2) },
            { "shaman", new ArchetypeStats(60, 1.0f, 2) }
        };

        public static SimulationRunResult SimulateSingleRun(LevelDefinition level, BotType bot, int seed, int runIndex)
        {
            var rng = new System.Random(seed + runIndex * 10007);
            int squad = level.InitialTroops > 0 ? level.InitialTroops : 5;
            float currentDist = 0f, fireRate = 2f, speed = level.Speed > 0f ? level.Speed : 8f;
            int damage = 2, kills = 0, mult = 0, rescued = 0;
            bool isAlive = true;
            int abilityKillsAccumulator = 0;
            int abilityCharges = 0;
            var stageModifiers = new StageModifierController();
            var pendingBonusZombiesByLane = new Dictionary<int, int>();

            if (level.Segments != null)
            {
                for (int s = 0; s < level.Segments.Length && isAlive; s++)
                {
                    var seg = level.Segments[s];
                    if (seg == null) continue;
                    if (seg.SegmentType == SegmentType.Warmup) currentDist = seg.StartDistance + seg.Length;
                    else if (seg.SegmentType == SegmentType.Gate)
                    {
                        ProcessGateSegment(seg, level, bot, ref squad, ref fireRate, ref damage, speed, stageModifiers, rng, ref isAlive, ref kills, currentDist);
                        currentDist = seg.StartDistance + seg.Length;
                    }
                    else if (seg.SegmentType == SegmentType.Horde)
                        ProcessHordeSegment(seg, level, bot, ref squad, fireRate, damage, speed, ref kills, ref currentDist, ref isAlive, rng, ref abilityKillsAccumulator, ref abilityCharges, stageModifiers, pendingBonusZombiesByLane);
                    else if (seg.SegmentType == SegmentType.Multiplier)
                        ProcessMultiplierSegment(seg, ref squad, ref mult, ref currentDist);
                    else if (seg.SegmentType == SegmentType.Cage)
                    {
                        ProcessCageSegment(seg, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
                        currentDist = seg.StartDistance + seg.Length;
                    }

                    if (seg.SegmentType != SegmentType.Cage && seg.Cages != null && seg.Cages.Length > 0)
                    {
                        ProcessCageSegment(seg, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
                    }
                }
            }

            if (isAlive && level.TotalDistance > 0f) currentDist = level.TotalDistance;
            return new SimulationRunResult
            {
                runIndex = runIndex, botType = bot.ToString(), victory = isAlive,
                distanceReached = currentDist, finalSquad = squad, multiplierReached = mult, kills = kills,
                rescued = rescued
            };
        }

        private static void ProcessGateSegment(
            SegmentDefinition seg, LevelDefinition level, BotType bot, ref int squad, ref float fireRate, ref int damage,
            float speed, StageModifierController stageModifiers, System.Random rng, ref bool isAlive, ref int kills, float currentDist)
        {
            if (seg.Events != null && seg.Events.Length > 0)
            {
                for (int i = 0; i < seg.Events.Length && isAlive; i++)
                {
                    if (seg.Events[i] != null)
                    {
                        ApplyGateEvent(seg.Events[i].Data, bot, ref squad, ref fireRate, ref damage, seg.Length, speed, stageModifiers, rng, level, ref isAlive, ref kills, currentDist);
                    }
                }
            }
            else if (bot == BotType.Guloso) { squad += 5; fireRate += 1f; }
        }

        private static void ApplyGateEvent(
            string data, BotType bot, ref int squad, ref float fireRate, ref int damage,
            float segmentLength, float speed, StageModifierController stageModifiers, System.Random rng,
            LevelDefinition level, ref bool isAlive, ref int kills, float currentDist)
        {
            if (string.IsNullOrEmpty(data)) { if (bot == BotType.Guloso) squad += 5; return; }

            var variant = ResolveGateVariant(data);
            if (variant != null)
            {
                ApplyVariantGateEvent(variant, data, bot, ref squad, ref fireRate, ref damage, segmentLength, speed, stageModifiers, rng, level, ref isAlive, ref kills, currentDist);
                return;
            }

            if (bot == BotType.Fujao)
            {
                if (data.Contains("subtract") || data.Contains("-")) squad = Math.Max(1, squad - 3);
                else if (data.Contains("divide") || data.Contains("/")) squad = Math.Max(1, squad / 2);
                return;
            }
            if (data.Contains("fireRate") || data.Contains("fire_rate"))
            {
                var m = Regex.Match(data, @"\+(\d+)");
                fireRate += m.Success ? float.Parse(m.Groups[1].Value) : 5f;
            }
            else if (data.Contains("damage"))
            {
                var m = Regex.Match(data, @"\+(\d+)");
                damage += m.Success ? int.Parse(m.Groups[1].Value) : 2;
            }
            else if (data.Contains("multiply") || data.Contains("*")) squad *= 2;
            else if (data.Contains("add") || data.Contains("+"))
            {
                var m = Regex.Match(data, @"\+?(\d+)");
                squad += m.Success ? int.Parse(m.Groups[1].Value) : 5;
            }
            else { squad += 5; fireRate += 2f; }
        }

        private static IGateVariant ResolveGateVariant(string data)
        {
            if (string.IsNullOrEmpty(data)) return null;

            string candidateId = data.Trim();
            var mId = Regex.Match(data, @"(?:gateId|gate|id)\s*=\s*([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
            if (mId.Success) candidateId = mId.Groups[1].Value;

            var definition = AssetDatabase.LoadAssetAtPath<GateDefinition>($"Assets/_Game/Data/Gates/{candidateId}.asset");
            if (definition == null)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<GateCatalog>("Assets/_Game/Data/Gates/GateCatalog.asset");
                if (catalog != null && catalog.TryGet(candidateId, out var catDef)) definition = catDef;
            }

            if (definition?.Variant != null) return definition.Variant;

            if (candidateId.StartsWith("gate_upgrade", StringComparison.OrdinalIgnoreCase) || data.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) >= 0)
                return new ShootToUpgradeGateVariant(-5, 1, 1, 15);
            if (candidateId.StartsWith("gate_cursed", StringComparison.OrdinalIgnoreCase) || data.IndexOf("cursed", StringComparison.OrdinalIgnoreCase) >= 0)
                return new CursedGateVariant("triple_frenzy", new StageModifierSpec("cursed", 1.35f, 1.2f), 150f);
            if (candidateId.StartsWith("gate_guarded", StringComparison.OrdinalIgnoreCase) || data.IndexOf("guarded", StringComparison.OrdinalIgnoreCase) >= 0)
                return new GuardedGateVariant("wave_guard", "runner", 5, 8f);
            if (candidateId.StartsWith("gate_static", StringComparison.OrdinalIgnoreCase))
                return new StaticGateVariant();

            return null;
        }

        private static void ApplyVariantGateEvent(
            IGateVariant variant, string data, BotType bot, ref int squad, ref float fireRate,
            ref int damage, float segmentLength, float speed, StageModifierController stageModifiers,
            System.Random rng, LevelDefinition level, ref bool isAlive, ref int kills, float currentDist)
        {
            if (variant is ShootToUpgradeGateVariant upgrade)
            {
                if (bot == BotType.Guloso)
                {
                    float approachDist = segmentLength > 0f ? segmentLength : 20f;
                    float approachTime = speed > 0f ? approachDist / speed : 2.5f;
                    int shots = (int)(fireRate * squad * approachTime);
                    int evolvedValue = upgrade.EvaluateValue(shots);
                    if (evolvedValue > 0) squad += evolvedValue;
                    else squad -= Math.Min(-evolvedValue, Math.Max(0, squad - 1));
                }
                else squad = upgrade.InitialValue < 0 ? Math.Max(1, squad - 3) : squad + upgrade.InitialValue;
            }
            else if (variant is CursedGateVariant cursed)
            {
                if (bot == BotType.Guloso)
                {
                    squad *= 3;
                    stageModifiers?.Activate(cursed.StageModifier, currentDist, cursed.DurationDistance);
                }
                else squad += 1;
            }
            else if (variant is GuardedGateVariant guarded)
            {
                if (bot == BotType.Guloso)
                {
                    int guardCount = guarded.GuardCount > 0 ? guarded.GuardCount : 5;
                    var stats = Archetypes.TryGetValue(guarded.GuardArchetypeId ?? "runner", out var a) ? a : new ArchetypeStats(12, 5.0f, 1);
                    float guardSpeed = stats.Speed * (stageModifiers != null ? stageModifiers.CurrentEnemySpeedMultiplier : 1f);
                    float closingTime = 40f / (speed + guardSpeed);
                    int killed = Math.Min(guardCount, (int)(fireRate * damage * squad * 0.95f * closingTime / stats.Hp));
                    kills += killed;
                    int reaching = guardCount - killed;
                    if (reaching > 0)
                    {
                        squad -= reaching * stats.ContactDamage;
                        if (squad <= 0) { squad = 0; isAlive = false; return; }
                    }
                    squad *= 2;
                }
                else squad += 1;
            }
            else
            {
                if (bot == BotType.Guloso) squad += 5;
                else squad += 1;
            }
        }

        private static void ProcessHordeSegment(
            SegmentDefinition seg, LevelDefinition level, BotType bot, ref int squad, float fireRate, int damage,
            float speed, ref int kills, ref float currentDist, ref bool isAlive, System.Random rng,
            ref int abilityKillsAccumulator, ref int abilityCharges, StageModifierController stageModifiers = null,
            Dictionary<int, int> pendingBonusZombiesByLane = null)
        {
            float hpMultiplier = level != null ? level.GetHpMultiplier() : 1f;
            var waves = new List<(float distance, int count, string archetype)>();
            if (seg.Events != null)
            {
                for (int i = 0; i < seg.Events.Length; i++)
                {
                    var e = seg.Events[i];
                    if (e == null || e.Type != "HordeSpawn") continue;
                    int count = 5;
                    string archetype = "walker";
                    int lane = e.Lane >= 0 ? e.Lane : 0;
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var mCount = Regex.Match(e.Data, @"count\s*=\s*(\d+)");
                        if (mCount.Success) count = int.Parse(mCount.Groups[1].Value);
                        var mArch = Regex.Match(e.Data, @"archetype\s*=\s*([a-zA-Z]+)");
                        if (mArch.Success) archetype = mArch.Groups[1].Value;
                        var mLane = Regex.Match(e.Data, @"lane\s*=\s*(\d+)");
                        if (mLane.Success) lane = int.Parse(mLane.Groups[1].Value);
                    }

                    if (pendingBonusZombiesByLane != null && pendingBonusZombiesByLane.TryGetValue(lane, out int bonus) && bonus > 0)
                    {
                        count += bonus;
                        pendingBonusZombiesByLane[lane] = 0;
                    }

                    waves.Add((seg.StartDistance + e.DistanceOffset, count, archetype));
                }
            }

            for (float offset = waves.Count > 0 ? 60f : 20f; offset < seg.Length - 10f; offset += 60f)
            {
                float wDist = seg.StartDistance + offset;
                bool near = false;
                for (int w = 0; w < waves.Count; w++) if (Math.Abs(waves[w].distance - wDist) < 20f) { near = true; break; }
                if (!near) waves.Add((wDist, 8 + rng.Next(0, 3), "walker"));
            }

            if (pendingBonusZombiesByLane != null)
            {
                foreach (var kvp in new List<KeyValuePair<int, int>>(pendingBonusZombiesByLane))
                {
                    if (kvp.Value > 0)
                    {
                        if (waves.Count > 0)
                        {
                            var first = waves[0];
                            waves[0] = (first.distance, first.count + kvp.Value, first.archetype);
                        }
                        pendingBonusZombiesByLane[kvp.Key] = 0;
                    }
                }
            }

            waves.Sort((a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < waves.Count; i++)
            {
                currentDist = waves[i].distance;
                if (stageModifiers != null) stageModifiers.TickDistance(currentDist);

                string archName = waves[i].archetype;
                var stats = Archetypes.TryGetValue(archName, out var a) ? a : new ArchetypeStats(20, 2.0f, 1);
                float speedMult = stageModifiers != null ? stageModifiers.CurrentEnemySpeedMultiplier : 1f;
                float contactMult = stageModifiers != null ? stageModifiers.CurrentEnemyContactDamageMultiplier : 1f;
                float effectiveHp = stats.Hp * hpMultiplier;
                float closingTime = 40f / (speed + stats.Speed * speedMult);

                float eff = (bot == BotType.Guloso) ? (0.95f + (float)(rng.NextDouble() * 0.08 - 0.04)) : (0.55f + (float)(rng.NextDouble() * 0.08 - 0.04));
                int killed = Math.Min(waves[i].count, (int)(fireRate * damage * squad * eff * closingTime / effectiveHp));
                kills += killed;
                abilityKillsAccumulator += killed;
                while (abilityKillsAccumulator >= 25)
                {
                    abilityKillsAccumulator -= 25;
                    abilityCharges++;
                }

                int reaching = waves[i].count - killed;
                if (reaching > 0 && abilityCharges > 0)
                {
                    abilityCharges--;
                    int grenadeKilled = Math.Min(reaching, Math.Max(1, (int)(150f / effectiveHp)));
                    kills += grenadeKilled;
                    reaching -= grenadeKilled;
                }

                if (reaching > 0)
                {
                    int contactDmg = (int)(stats.ContactDamage * contactMult);
                    squad -= reaching * Math.Max(1, contactDmg);
                    if (squad <= 0) { squad = 0; isAlive = false; return; }
                }
            }
            currentDist = seg.StartDistance + seg.Length;
            if (stageModifiers != null) stageModifiers.TickDistance(currentDist);
        }

        private static void ProcessMultiplierSegment(SegmentDefinition seg, ref int squad, ref int mult, ref float currentDist)
        {
            int[] costs = { 5, 10, 15, 20, 25 };
            for (int i = 0; i < costs.Length; i++)
            {
                if (squad < costs[i]) break;
                squad -= costs[i];
                mult = i + 1;
                currentDist += 15f;
            }
        }

        private static void ProcessCageSegment(
            SegmentDefinition seg, BotType bot, ref int squad, float fireRate, int damage,
            float speed, ref int rescued, Dictionary<int, int> pendingBonusZombiesByLane)
        {
            bool processed = false;
            if (seg.Cages != null && seg.Cages.Length > 0)
            {
                for (int i = 0; i < seg.Cages.Length; i++)
                {
                    var cage = seg.Cages[i];
                    if (cage != null)
                    {
                        ProcessSingleCage(cage.CageId, cage.LaneIndex, cage.CustomHp, cage.CustomSurvivors, seg.Length, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
                        processed = true;
                    }
                }
            }

            if (seg.Events != null && seg.Events.Length > 0)
            {
                for (int i = 0; i < seg.Events.Length; i++)
                {
                    var e = seg.Events[i];
                    if (e != null && (string.Equals(e.Type, "Cage", StringComparison.OrdinalIgnoreCase) || seg.SegmentType == SegmentType.Cage))
                    {
                        ParseAndProcessCageEvent(e.Data, e.Lane, seg.Length, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
                        processed = true;
                    }
                }
            }

            if (!processed && seg.SegmentType == SegmentType.Cage)
            {
                ProcessSingleCage("cage_standard_3", 0, 0, 0, seg.Length, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
            }
        }

        private static void ParseAndProcessCageEvent(
            string data, int eventLane, float segmentLength, BotType bot, ref int squad,
            float fireRate, int damage, float speed, ref int rescued, Dictionary<int, int> pendingBonusZombiesByLane)
        {
            string cageId = "cage_standard_3";
            int customHp = 0;
            int customSurvivors = 0;
            int lane = eventLane >= 0 ? eventLane : 0;

            if (!string.IsNullOrEmpty(data))
            {
                var mId = Regex.Match(data, @"(?:cageId|cage|id)\s*=\s*([a-zA-Z0-9_-]+)", RegexOptions.IgnoreCase);
                if (mId.Success) cageId = mId.Groups[1].Value;
                else if (!data.Contains("=")) cageId = data.Trim();

                var mHp = Regex.Match(data, @"(?:hp|maxHp)\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                if (mHp.Success) customHp = int.Parse(mHp.Groups[1].Value);

                var mSurv = Regex.Match(data, @"(?:survivors|count)\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                if (mSurv.Success) customSurvivors = int.Parse(mSurv.Groups[1].Value);

                var mLane = Regex.Match(data, @"lane\s*=\s*(\d+)", RegexOptions.IgnoreCase);
                if (mLane.Success) lane = int.Parse(mLane.Groups[1].Value);
            }

            ProcessSingleCage(cageId, lane, customHp, customSurvivors, segmentLength, bot, ref squad, fireRate, damage, speed, ref rescued, pendingBonusZombiesByLane);
        }

        private static void ProcessSingleCage(
            string cageId, int lane, int customHp, int customSurvivors, float segmentLength,
            BotType bot, ref int squad, float fireRate, int damage, float speed,
            ref int rescued, Dictionary<int, int> pendingBonusZombiesByLane)
        {
            var def = ResolveCageDefinition(cageId);
            int hp = customHp > 0 ? customHp : (def != null && def.MaxHp > 0 ? def.MaxHp : 60);
            int survivors = customSurvivors > 0 ? customSurvivors : (def != null && def.SurvivorCount > 0 ? def.SurvivorCount : 3);
            int cageLane = lane >= 0 ? lane : 0;

            float approachDist = segmentLength > 0f ? segmentLength : 20f;
            float approachTime = speed > 0f ? approachDist / speed : 2.5f;
            float potentialDamage = fireRate * damage * squad * approachTime;

            if (bot == BotType.Guloso && potentialDamage >= hp)
            {
                squad += survivors;
                rescued += survivors;
            }
            else
            {
                if (pendingBonusZombiesByLane != null)
                {
                    pendingBonusZombiesByLane[cageLane] = pendingBonusZombiesByLane.TryGetValue(cageLane, out int cur) ? cur + survivors : survivors;
                }
            }
        }

        private static CageDefinition ResolveCageDefinition(string cageId)
        {
            if (string.IsNullOrEmpty(cageId)) return null;

            var definition = AssetDatabase.LoadAssetAtPath<CageDefinition>($"Assets/_Game/Data/Cages/{cageId}.asset");
            if (definition == null)
            {
                var catalog = AssetDatabase.LoadAssetAtPath<CageCatalog>("Assets/_Game/Data/Cages/CageCatalog.asset");
                if (catalog != null && catalog.TryGet(cageId, out var catDef)) definition = catDef;
            }
            return definition;
        }

        public static int ExecuteFromCommandLine(string[] args = null)
        {
            string[] cmd = args ?? Environment.GetCommandLineArgs();
            string levelId = "level_01";
            int seed = 42, runs = 10;
            string outputPath = null;

            if (cmd != null)
            {
                for (int i = 0; i < cmd.Length; i++)
                {
                    if (cmd[i].Equals("-level", StringComparison.OrdinalIgnoreCase) && i + 1 < cmd.Length) levelId = cmd[i + 1];
                    else if (cmd[i].Equals("-seed", StringComparison.OrdinalIgnoreCase) && i + 1 < cmd.Length) int.TryParse(cmd[i + 1], out seed);
                    else if (cmd[i].Equals("-runs", StringComparison.OrdinalIgnoreCase) && i + 1 < cmd.Length) int.TryParse(cmd[i + 1], out runs);
                    else if (cmd[i].Equals("-output", StringComparison.OrdinalIgnoreCase) && i + 1 < cmd.Length) outputPath = cmd[i + 1];
                }
            }

            try
            {
                var report = RunAll(levelId, seed, runs, outputPath);
                for (int i = 0; i < report.summaries.Count; i++)
                {
                    var s = report.summaries[i];
                    Debug.Log($"[SimulateCommand] Bot {s.botType}: WinRate={s.winRate:P0} ({s.victories}/{s.totalRuns}), AvgDist={s.averageDistance:F1}m, AvgSquad={s.averageFinalSquad:F1}");
                }
                Debug.Log($"[SimulateCommand] Simulation finished successfully for level '{levelId}' (seed={seed}, runs={runs}).");
                return 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimulateCommand] Simulation failed: {ex.Message}\n{ex.StackTrace}");
                return 1;
            }
        }
    }
}
