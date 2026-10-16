using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Meta.Objectives
{
    public class ObjectiveTracker : IObjectiveTracker
    {
        private readonly IEventBus _eventBus;
        private readonly Dictionary<string, ObjectiveDefinition> _definitions = new();
        private readonly Dictionary<string, ObjectiveProgress> _progress = new();
        private readonly HashSet<string> _completedEmitted = new();

        public ObjectiveTracker(IEventBus eventBus = null)
        {
            _eventBus = eventBus;
            if (_eventBus != null)
            {
                _eventBus.Subscribe<EnemyKilledEvent>(IngestEvent);
                _eventBus.Subscribe<SurvivorsRescuedEvent>(IngestEvent);
                _eventBus.Subscribe<GatePassedEvent>(IngestEvent);
            }
        }

        public void RegisterObjective(ObjectiveDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _definitions[definition.Id] = definition;
            if (!_progress.ContainsKey(definition.Id))
            {
                _progress[definition.Id] = new ObjectiveProgress(definition.Id, definition.TargetValue, 0);
            }
        }

        public ObjectiveProgress GetProgress(string objectiveId)
        {
            if (string.IsNullOrEmpty(objectiveId)) return null;
            return _progress.TryGetValue(objectiveId, out var prog) ? prog : null;
        }

        public void IngestEvent<TEvent>(TEvent evt)
        {
            if (evt == null) return;

            if (evt is EnemyKilledEvent enemyKilled)
            {
                IncrementMatching("kills", 1, enemyKilled.ArchetypeId);
            }
            else if (evt is SurvivorsRescuedEvent survivorsRescued)
            {
                IncrementMatching("rescues", survivorsRescued.Count);
            }
            else if (evt is GatePassedEvent)
            {
                IncrementMatching("gates", 1);
            }
            else if (evt is AbilityUsedEvent)
            {
                IncrementMatching("ability_used", 1);
            }
            else if (evt is SynergyTriggeredEvent)
            {
                IncrementMatching("synergies", 1);
            }
        }

        public void IngestRunResult(RunResult result)
        {
            if (result == null) return;

            foreach (var kvp in _definitions)
            {
                var def = kvp.Value;
                var prog = _progress[def.Id];

                if (def.Scope == ObjectiveScope.Run)
                {
                    int runValue = EvaluateRunMetric(def, result);
                    prog.CurrentValue = runValue;
                }
                else if (def.Scope == ObjectiveScope.Cumulative)
                {
                    int addValue = EvaluateCumulativePostRunMetric(def, result);
                    prog.CurrentValue += addValue;
                }

                CheckAndNotifyCompletion(def.Id, prog);
            }
        }

        public IReadOnlyList<string> CheckCompletedObjectives()
        {
            var completed = new List<string>();
            foreach (var kvp in _progress)
            {
                if (kvp.Value.IsCompleted)
                {
                    completed.Add(kvp.Key);
                }
            }
            return completed;
        }

        private void IncrementMatching(string metric, int amount, string filterValue = null)
        {
            foreach (var kvp in _definitions)
            {
                var def = kvp.Value;
                if (!string.Equals(def.Metric, metric, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(def.Filter) && !string.Equals(def.Filter, filterValue, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var prog = _progress[def.Id];
                prog.CurrentValue += amount;
                CheckAndNotifyCompletion(def.Id, prog);
            }
        }

        private static int EvaluateRunMetric(ObjectiveDefinition def, RunResult result)
        {
            return def.Metric.ToLowerInvariant() switch
            {
                "stars" => result.Stars,
                "coins" => result.CoinsEarned,
                "victory" or "wins" => result.Victory ? 1 : 0,
                "distance" => (int)result.Distance,
                "rescues" => result.Rescued,
                "multiplier" => result.MultiplierReached,
                "holdouts" => result.HoldoutsHeld,
                "kills" => GetKills(def, result),
                _ => 0
            };
        }

        private static int EvaluateCumulativePostRunMetric(ObjectiveDefinition def, RunResult result)
        {
            return def.Metric.ToLowerInvariant() switch
            {
                "stars" => result.Stars,
                "coins" => result.CoinsEarned,
                "victory" or "wins" => result.Victory ? 1 : 0,
                _ => 0
            };
        }

        private static int GetKills(ObjectiveDefinition def, RunResult result)
        {
            if (result.KillsByArchetype == null) return 0;
            if (!string.IsNullOrEmpty(def.Filter))
            {
                return result.KillsByArchetype.TryGetValue(def.Filter, out int k) ? k : 0;
            }

            int total = 0;
            foreach (var count in result.KillsByArchetype.Values)
            {
                total += count;
            }
            return total;
        }

        private void CheckAndNotifyCompletion(string id, ObjectiveProgress progress)
        {
            if (progress.IsCompleted && _completedEmitted.Add(id))
            {
                _eventBus?.Publish(new ObjectiveCompletedEvent(id));
            }
        }
    }
}
