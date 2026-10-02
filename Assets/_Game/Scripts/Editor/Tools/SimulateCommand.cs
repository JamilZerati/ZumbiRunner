using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Game.Data;

namespace Game.Editor.Tools
{
    public enum BotType { Guloso, Fujao }

    [Serializable]
    public class SimulationRunResult
    {
        public int runIndex, finalSquad, multiplierReached, kills;
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
    }

    [Serializable]
    public class SimulationSummary
    {
        public string botType;
        public int totalRuns, victories;
        public float winRate, averageDistance, averageFinalSquad, averageMultiplier;
        public List<SimulationRunResult> runs = new List<SimulationRunResult>();

        public BotType Bot => (BotType)Enum.Parse(typeof(BotType), botType);
        public int TotalRuns => totalRuns;
        public int Victories => victories;
        public float WinRate => winRate;
        public float AverageDistance => averageDistance;
        public float AverageFinalSquad => averageFinalSquad;
        public float AverageMultiplier => averageMultiplier;
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
            int totalSquad = 0, totalMultiplier = 0;

            for (int i = 0; i < runs; i++)
            {
                var r = SimulateSingleRun(level, botType, seed, i);
                summary.runs.Add(r);
                if (r.victory) summary.victories++;
                totalDistance += r.distanceReached;
                totalSquad += r.finalSquad;
                totalMultiplier += r.multiplierReached;
            }

            summary.winRate = runs > 0 ? (float)summary.victories / runs : 0f;
            summary.averageDistance = runs > 0 ? totalDistance / runs : 0f;
            summary.averageFinalSquad = runs > 0 ? (float)totalSquad / runs : 0f;
            summary.averageMultiplier = runs > 0 ? (float)totalMultiplier / runs : 0f;
            return summary;
        }

        public static SimulationRunResult SimulateSingleRun(LevelDefinition level, BotType bot, int seed, int runIndex)
        {
            var rng = new System.Random(seed + runIndex * 10007);
            int squad = level.InitialTroops > 0 ? level.InitialTroops : 5;
            float currentDist = 0f, fireRate = 2f, speed = level.Speed > 0f ? level.Speed : 8f, zombieSpeed = 2f;
            int damage = 2, kills = 0, mult = 0;
            bool isAlive = true;

            if (level.Segments != null)
            {
                for (int s = 0; s < level.Segments.Length && isAlive; s++)
                {
                    var seg = level.Segments[s];
                    if (seg == null) continue;
                    if (seg.SegmentType == SegmentType.Warmup) currentDist = seg.StartDistance + seg.Length;
                    else if (seg.SegmentType == SegmentType.Gate)
                    {
                        ProcessGateSegment(seg, bot, ref squad, ref fireRate, ref damage);
                        currentDist = seg.StartDistance + seg.Length;
                    }
                    else if (seg.SegmentType == SegmentType.Horde)
                        ProcessHordeSegment(seg, bot, ref squad, fireRate, damage, speed, zombieSpeed, ref kills, ref currentDist, ref isAlive, rng);
                    else if (seg.SegmentType == SegmentType.Multiplier)
                        ProcessMultiplierSegment(seg, ref squad, ref mult, ref currentDist);
                }
            }

            if (isAlive && level.TotalDistance > 0f) currentDist = level.TotalDistance;
            return new SimulationRunResult
            {
                runIndex = runIndex, botType = bot.ToString(), victory = isAlive,
                distanceReached = currentDist, finalSquad = squad, multiplierReached = mult, kills = kills
            };
        }

        private static void ProcessGateSegment(SegmentDefinition seg, BotType bot, ref int squad, ref float fireRate, ref int damage)
        {
            if (seg.Events != null && seg.Events.Length > 0)
            {
                for (int i = 0; i < seg.Events.Length; i++)
                    if (seg.Events[i] != null) ApplyGateEvent(seg.Events[i].Data, bot, ref squad, ref fireRate, ref damage);
            }
            else if (bot == BotType.Guloso) { squad += 5; fireRate += 1f; }
        }

        private static void ApplyGateEvent(string data, BotType bot, ref int squad, ref float fireRate, ref int damage)
        {
            if (string.IsNullOrEmpty(data)) { if (bot == BotType.Guloso) squad += 5; return; }
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

        private static void ProcessHordeSegment(
            SegmentDefinition seg, BotType bot, ref int squad, float fireRate, int damage,
            float speed, float zombieSpeed, ref int kills, ref float currentDist, ref bool isAlive, System.Random rng)
        {
            var waves = new List<(float distance, int count)>();
            if (seg.Events != null)
            {
                var eventGroups = new Dictionary<float, int>();
                for (int i = 0; i < seg.Events.Length; i++)
                {
                    var e = seg.Events[i];
                    if (e == null || e.Type != "HordeSpawn") continue;
                    int count = 5;
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        var m = Regex.Match(e.Data, @"count\s*=\s*(\d+)");
                        if (m.Success) count = int.Parse(m.Groups[1].Value);
                    }
                    eventGroups[e.DistanceOffset] = eventGroups.TryGetValue(e.DistanceOffset, out int c) ? c + count : count;
                }
                foreach (var kvp in eventGroups) waves.Add((seg.StartDistance + kvp.Key, kvp.Value));
            }

            for (float offset = waves.Count > 0 ? 60f : 20f; offset < seg.Length - 10f; offset += 60f)
            {
                float wDist = seg.StartDistance + offset;
                bool near = false;
                for (int w = 0; w < waves.Count; w++) if (Math.Abs(waves[w].distance - wDist) < 20f) { near = true; break; }
                if (!near) waves.Add((wDist, 8 + rng.Next(0, 3)));
            }
            waves.Sort((a, b) => a.distance.CompareTo(b.distance));
            float closingTime = 40f / (speed + zombieSpeed);

            for (int i = 0; i < waves.Count; i++)
            {
                currentDist = waves[i].distance;
                float eff = (bot == BotType.Guloso) ? (0.95f + (float)(rng.NextDouble() * 0.08 - 0.04)) : (0.55f + (float)(rng.NextDouble() * 0.08 - 0.04));
                int killed = Math.Min(waves[i].count, (int)(fireRate * damage * squad * eff * closingTime / 20f));
                kills += killed;
                int reaching = waves[i].count - killed;
                if (reaching > 0)
                {
                    squad -= reaching;
                    if (squad <= 0) { squad = 0; isAlive = false; return; }
                }
            }
            currentDist = seg.StartDistance + seg.Length;
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
