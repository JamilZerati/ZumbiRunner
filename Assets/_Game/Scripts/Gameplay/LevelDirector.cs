using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Core.Events;
using Game.Core.State;
using Game.Data;

namespace Game.Gameplay
{
    public class LevelDirector : MonoBehaviour
    {
        public const float HordeSpawnAdvanceDistance = 40.0f;
        public const float EnemyEscapeDistanceBehindGeneral = 5.0f;

        private RunConfig _config;
        private LevelDefinition _levelDef;
        private IEventBus _eventBus;

        private bool _isInitialized;
        private bool _isRunActive;
        private float _currentDistance;
        private int _currentSquad;
        private int _multiplierReached;
        private readonly Dictionary<string, int> _killsByArchetype = new Dictionary<string, int>();
        private readonly HashSet<SegmentDefinition> _spawnedSegments = new HashSet<SegmentDefinition>();
        private readonly HashSet<SegmentEvent> _spawnedEvents = new HashSet<SegmentEvent>();
        private readonly List<EnemyController> _trackedEnemies = new List<EnemyController>();

        private IDisposable _squadSub;
        private IDisposable _enemyKilledSub;
        private IDisposable _multiplierSub;

        public RunConfig Config => _config;
        public LevelDefinition LevelDefinition => _levelDef;
        public bool IsRunActive => _isRunActive;
        public float CurrentDistance => _currentDistance;
        public int CurrentSquad => _currentSquad;
        public int MultiplierReached => _multiplierReached;
        public int EscapedEnemyCount { get; private set; }
        public int SpawnedHordeCount { get; private set; }
        public IReadOnlyDictionary<string, int> KillsByArchetype => _killsByArchetype;

        public event Action<SegmentDefinition> OnHordeSpawned;
        public event Action<SegmentDefinition, SegmentEvent> OnSegmentEventTriggered;
        public event Action<RunResult> OnRunEnded;

        public void Initialize(RunConfig config)
        {
            Initialize(config, null, null);
        }

        public void Initialize(RunConfig config, LevelDefinition levelDef, IEventBus eventBus)
        {
            CleanupSubscriptions();

            _config = config;
            _levelDef = levelDef;
            _eventBus = eventBus;

            _currentDistance = 0f;
            _currentSquad = config != null ? config.InitialSquad : (levelDef != null ? levelDef.InitialTroops : 10);
            _multiplierReached = 0;
            EscapedEnemyCount = 0;
            SpawnedHordeCount = 0;

            _killsByArchetype.Clear();
            _spawnedSegments.Clear();
            _spawnedEvents.Clear();
            _trackedEnemies.Clear();

            _isInitialized = true;
            _isRunActive = true;

            if (_eventBus != null)
            {
                _squadSub = _eventBus.Subscribe<SquadSizeChangedEvent>(OnSquadSizeChanged);
                _enemyKilledSub = _eventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
                _multiplierSub = _eventBus.Subscribe<MultiplierReachedEvent>(OnMultiplierReached);

                string levelId = _levelDef != null ? _levelDef.LevelId : (_config != null ? _config.LevelId : string.Empty);
                _eventBus.Publish(new RunStartedEvent(levelId, _config));
            }
        }

        public void Tick(float distance)
        {
            if (!_isInitialized || !_isRunActive)
            {
                return;
            }

            _currentDistance = distance;

            CheckSpawns(distance);
            CheckTrackedEnemies(distance);

            if (_levelDef != null && _levelDef.TotalDistance > 0f)
            {
                if (distance >= _levelDef.TotalDistance)
                {
                    EndRun(victory: true);
                }
            }
        }

        private void CheckSpawns(float distance)
        {
            if (_levelDef == null || _levelDef.Segments == null)
            {
                return;
            }

            for (int i = 0; i < _levelDef.Segments.Length; i++)
            {
                var segment = _levelDef.Segments[i];
                if (segment == null)
                {
                    continue;
                }

                if (segment.SegmentType == SegmentType.Horde)
                {
                    float hordeTrigger = segment.StartDistance - HordeSpawnAdvanceDistance;
                    if (distance >= hordeTrigger && !_spawnedSegments.Contains(segment))
                    {
                        _spawnedSegments.Add(segment);
                        SpawnedHordeCount++;
                        OnHordeSpawned?.Invoke(segment);
                    }
                }

                if (segment.Events != null)
                {
                    for (int j = 0; j < segment.Events.Length; j++)
                    {
                        var evt = segment.Events[j];
                        if (evt == null || _spawnedEvents.Contains(evt))
                        {
                            continue;
                        }

                        float evtTrigger = (segment.StartDistance + evt.DistanceOffset) - HordeSpawnAdvanceDistance;
                        if (distance >= evtTrigger)
                        {
                            _spawnedEvents.Add(evt);
                            OnSegmentEventTriggered?.Invoke(segment, evt);
                        }
                    }
                }
            }
        }

        public bool NotifyEnemyPosition(string archetypeId, int lane, float enemyZ, float generalZ)
        {
            if (generalZ - enemyZ >= EnemyEscapeDistanceBehindGeneral)
            {
                EscapedEnemyCount++;
                _eventBus?.Publish(new EnemyEscapedEvent(archetypeId, lane, enemyZ));
                return true;
            }
            return false;
        }

        public void CheckEnemyPositions(IEnumerable<EnemyController> enemies, float generalZ)
        {
            if (enemies == null)
            {
                return;
            }

            var list = new List<EnemyController>(enemies);
            for (int i = 0; i < list.Count; i++)
            {
                var enemy = list[i];
                if (enemy == null || !enemy.IsActiveInPool)
                {
                    continue;
                }

                float enemyZ = enemy.transform.position.z;
                if (generalZ - enemyZ >= EnemyEscapeDistanceBehindGeneral)
                {
                    EscapedEnemyCount++;
                    _eventBus?.Publish(new EnemyEscapedEvent(enemy.ArchetypeId, enemy.LaneIndex, enemyZ));
                    enemy.Recycle();
                }
            }
        }

        public void TrackEnemy(EnemyController enemy)
        {
            if (enemy != null && !_trackedEnemies.Contains(enemy))
            {
                _trackedEnemies.Add(enemy);
            }
        }

        private void CheckTrackedEnemies(float generalZ)
        {
            for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _trackedEnemies[i];
                if (enemy == null || !enemy.IsActiveInPool)
                {
                    _trackedEnemies.RemoveAt(i);
                    continue;
                }

                float enemyZ = enemy.transform.position.z;
                if (generalZ - enemyZ >= EnemyEscapeDistanceBehindGeneral)
                {
                    _trackedEnemies.RemoveAt(i);
                    EscapedEnemyCount++;
                    _eventBus?.Publish(new EnemyEscapedEvent(enemy.ArchetypeId, enemy.LaneIndex, enemyZ));
                    enemy.Recycle();
                }
            }
        }

        public bool HasSpawnedSegment(SegmentDefinition segment)
        {
            return segment != null && _spawnedSegments.Contains(segment);
        }

        public void EndRun(bool victory)
        {
            if (!_isRunActive)
            {
                return;
            }

            _isRunActive = false;

            string levelId = _levelDef != null ? _levelDef.LevelId : (_config != null ? _config.LevelId : string.Empty);
            var result = new RunResult
            {
                LevelId = levelId,
                Victory = victory,
                Distance = _currentDistance,
                SquadAtEnd = _currentSquad,
                MultiplierReached = _multiplierReached,
                KillsByArchetype = new Dictionary<string, int>(_killsByArchetype),
                DurationSeconds = 0f,
                Stars = victory ? (_currentSquad > 0 ? 3 : 1) : 0,
                MutationIds = _config != null && _config.MutationIds != null ? new List<string>(_config.MutationIds) : new List<string>()
            };

            OnRunEnded?.Invoke(result);
            _eventBus?.Publish(new RunEndedEvent(result));
        }

        private void OnSquadSizeChanged(SquadSizeChangedEvent evt)
        {
            _currentSquad = evt.NewCount;
            if (_currentSquad <= 0 && _isRunActive)
            {
                EndRun(victory: false);
            }
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (string.IsNullOrEmpty(evt.ArchetypeId))
            {
                return;
            }

            if (_killsByArchetype.TryGetValue(evt.ArchetypeId, out int count))
            {
                _killsByArchetype[evt.ArchetypeId] = count + 1;
            }
            else
            {
                _killsByArchetype[evt.ArchetypeId] = 1;
            }
        }

        private void OnMultiplierReached(MultiplierReachedEvent evt)
        {
            _multiplierReached = evt.Multiplier;
        }

        private void CleanupSubscriptions()
        {
            _squadSub?.Dispose();
            _squadSub = null;
            _enemyKilledSub?.Dispose();
            _enemyKilledSub = null;
            _multiplierSub?.Dispose();
            _multiplierSub = null;
        }

        private void OnDestroy()
        {
            CleanupSubscriptions();
        }
    }
}
