using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core;
using Game.Core.Boss;
using Game.Core.Events;
using Game.Core.Gates;
using Game.Core.State;
using Game.Core.Weather;
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
        private readonly List<Cage> _trackedCages = new List<Cage>();
        private readonly Dictionary<int, int> _pendingBonusZombiesByLane = new Dictionary<int, int>();
        private int _rescuedSurvivorsCount;
        private int _holdoutsAttempted;
        private int _holdoutsHeld;

        private IDisposable _squadSub;
        private IDisposable _enemyKilledSub;
        private IDisposable _multiplierSub;
        private IDisposable _survivorsRescuedSub;
        private IDisposable _survivorsLostSub;
        private IDisposable _holdoutStartedSub;
        private IDisposable _holdoutEndedSub;
        private IDisposable _bossDefeatedSub;

        public RunConfig Config => _config;
        public LevelDefinition LevelDefinition => _levelDef;
        public bool IsRunActive => _isRunActive;
        public float CurrentDistance => _currentDistance;
        public int CurrentSquad => _currentSquad;
        public int MultiplierReached => _multiplierReached;
        public int EscapedEnemyCount { get; private set; }
        public int SpawnedHordeCount { get; private set; }
        public int SpawnedHoldoutCount { get; private set; }
        public int SpawnedBossCount { get; private set; }
        public bool IsBossFightActive { get; private set; }
        public string ActiveBossId { get; private set; }
        public BossCombatRuntimeState BossCombatState { get; private set; }
        public int RescuedSurvivorsCount => _rescuedSurvivorsCount;
        public int HoldoutsAttempted => _holdoutsAttempted;
        public int HoldoutsHeld => _holdoutsHeld;
        public IReadOnlyDictionary<string, int> KillsByArchetype => _killsByArchetype;

        private IWeatherSystem _weatherSystem;
        private StageModifierController _stageModifierController;

        public IWeatherSystem WeatherSystem
        {
            get => _weatherSystem ??= new WeatherSystem(_eventBus);
            set => _weatherSystem = value;
        }

        public StageModifierController StageModifierController
        {
            get => _stageModifierController ??= new StageModifierController();
            set => _stageModifierController = value;
        }

        public int GetPendingBonusZombies(int lane)
        {
            return _pendingBonusZombiesByLane.TryGetValue(lane, out int count) ? count : 0;
        }

        public int ConsumePendingBonusZombies(int lane)
        {
            if (_pendingBonusZombiesByLane.TryGetValue(lane, out int count))
            {
                _pendingBonusZombiesByLane[lane] = 0;
                return count;
            }
            return 0;
        }

        public event Action<SegmentDefinition> OnHordeSpawned;
        public event Action<SegmentDefinition> OnHoldoutSpawned;
        public event Action<SegmentDefinition> OnBossSpawned;
        public event Action<string> OnBossFightEnded;
        public event Action<SegmentDefinition, SegmentEvent> OnSegmentEventTriggered;
        public event Action<RunResult> OnRunEnded;

        public void Initialize(RunConfig config)
        {
            Initialize(config, null, null, null, null);
        }

        public void Initialize(RunConfig config, LevelDefinition levelDef, IEventBus eventBus)
        {
            Initialize(config, levelDef, eventBus, null, null);
        }

        public void Initialize(RunConfig config, LevelDefinition levelDef, IEventBus eventBus,
                               IWeatherSystem weatherSystem = null, StageModifierController stageModifiers = null)
        {
            CleanupSubscriptions();

            _config = config;
            _levelDef = levelDef;
            _eventBus = eventBus;
            _weatherSystem = weatherSystem ?? new WeatherSystem(eventBus);
            _stageModifierController = stageModifiers ?? new StageModifierController();

            _currentDistance = 0f;
            _currentSquad = config != null ? config.InitialSquad : (levelDef != null ? levelDef.InitialTroops : 10);
            _multiplierReached = 0;
            EscapedEnemyCount = 0;
            SpawnedHordeCount = 0;
            SpawnedHoldoutCount = 0;
            SpawnedBossCount = 0;
            IsBossFightActive = false;
            ActiveBossId = null;
            BossCombatState = null;
            _holdoutsAttempted = 0;
            _holdoutsHeld = 0;

            _killsByArchetype.Clear();
            _spawnedSegments.Clear();
            _spawnedEvents.Clear();
            _trackedEnemies.Clear();
            _trackedCages.Clear();
            _rescuedSurvivorsCount = 0;
            _pendingBonusZombiesByLane.Clear();

            _isInitialized = true;
            _isRunActive = true;

            if (_eventBus != null)
            {
                _squadSub = _eventBus.Subscribe<SquadSizeChangedEvent>(OnSquadSizeChanged);
                _enemyKilledSub = _eventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
                _multiplierSub = _eventBus.Subscribe<MultiplierReachedEvent>(OnMultiplierReached);
                _survivorsRescuedSub = _eventBus.Subscribe<SurvivorsRescuedEvent>(OnSurvivorsRescued);
                _survivorsLostSub = _eventBus.Subscribe<SurvivorsLostEvent>(OnSurvivorsLost);
                _holdoutStartedSub = _eventBus.Subscribe<HoldoutStartedEvent>(OnHoldoutStarted);
                _holdoutEndedSub = _eventBus.Subscribe<HoldoutEndedEvent>(OnHoldoutEnded);
                _bossDefeatedSub = _eventBus.Subscribe<BossDefeatedEvent>(OnBossDefeated);

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

            _weatherSystem?.TickDistance(distance, _stageModifierController);

            CheckSpawns(distance);
            CheckTrackedEnemies(distance);
            CheckTrackedCages(distance);

            if (_levelDef != null && _levelDef.TotalDistance > 0f)
            {
                if (distance >= _levelDef.TotalDistance && !IsBossFightActive)
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

                if (segment.Weather != WeatherType.Clear && distance >= segment.StartDistance && !_spawnedSegments.Contains(segment))
                {
                    _weatherSystem?.SetWeather(segment.Weather, segment.Length > 0f ? segment.Length : 50f, _stageModifierController);
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
                else if (segment.SegmentType == SegmentType.Holdout)
                {
                    if (distance >= segment.StartDistance && !_spawnedSegments.Contains(segment))
                    {
                        _spawnedSegments.Add(segment);
                        SpawnedHoldoutCount++;
                        OnHoldoutSpawned?.Invoke(segment);
                    }
                }
                else if (segment.SegmentType == SegmentType.Boss)
                {
                    if (distance >= segment.StartDistance && !_spawnedSegments.Contains(segment))
                    {
                        _spawnedSegments.Add(segment);
                        SpawnedBossCount++;
                        IsBossFightActive = true;

                        string bossId = segment.Boss?.BossId ?? (_levelDef != null ? _levelDef.BossId : null) ?? "boss_colossus";
                        int maxHp = segment.Boss != null && segment.Boss.CustomMaxHp > 0 ? segment.Boss.CustomMaxHp : 1000;
                        int initialLane = segment.Boss != null ? segment.Boss.InitialLane : 0;

                        ActiveBossId = bossId;
                        BossCombatState = new BossCombatRuntimeState();
                        BossCombatState.StartArena(bossId, maxHp, initialLane);
                        BossCombatState.OnBossDied += OnBossCombatStateDied;

                        OnBossSpawned?.Invoke(segment);
                        _eventBus?.Publish(new BossSpawnedEvent(bossId, distance, maxHp));
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
                            if (evt.Type != null && evt.Type.Equals("Weather", StringComparison.OrdinalIgnoreCase))
                            {
                                if (Enum.TryParse<WeatherType>(evt.Data, true, out var wType))
                                {
                                    _weatherSystem?.SetWeather(wType, segment.Length > 0f ? segment.Length : 50f, _stageModifierController);
                                }
                            }
                            OnSegmentEventTriggered?.Invoke(segment, evt);
                        }
                    }
                }
            }
        }

        public bool IsBossArchetype(string archetypeId)
        {
            if (string.IsNullOrEmpty(archetypeId)) return false;
            return string.Equals(archetypeId, ActiveBossId, StringComparison.OrdinalIgnoreCase) ||
                   archetypeId.StartsWith("boss_", StringComparison.OrdinalIgnoreCase);
        }

        public bool CanEnemyEscape(string archetypeId, float enemyZ, float generalZ)
        {
            if (IsBossFightActive && IsBossArchetype(archetypeId))
            {
                return false;
            }
            return (generalZ - enemyZ) >= EnemyEscapeDistanceBehindGeneral;
        }

        public bool NotifyEnemyPosition(string archetypeId, int lane, float enemyZ, float generalZ)
        {
            if (!CanEnemyEscape(archetypeId, enemyZ, generalZ))
            {
                return false;
            }

            EscapedEnemyCount++;
            _eventBus?.Publish(new EnemyEscapedEvent(archetypeId, lane, enemyZ));
            return true;
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

                if (IsBossFightActive && IsBossArchetype(enemy.ArchetypeId))
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

                if (IsBossFightActive && IsBossArchetype(enemy.ArchetypeId))
                {
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

        public void TrackCage(Cage cage)
        {
            if (cage != null && !_trackedCages.Contains(cage))
            {
                _trackedCages.Add(cage);
            }
        }

        private void CheckTrackedCages(float generalZ)
        {
            for (int i = _trackedCages.Count - 1; i >= 0; i--)
            {
                var cage = _trackedCages[i];
                if (cage == null || !cage.gameObject.activeInHierarchy || !cage.IsAlive || cage.CheckEscape(generalZ))
                {
                    _trackedCages.RemoveAt(i);
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

            int stars = 0;
            if (victory)
            {
                stars = _currentSquad > 0 ? 3 : 1;
                if (_holdoutsAttempted > 0 && _holdoutsHeld < _holdoutsAttempted)
                {
                    stars = Mathf.Min(stars, 2);
                }
            }

            string levelId = _levelDef != null ? _levelDef.LevelId : (_config != null ? _config.LevelId : string.Empty);
            var result = new RunResult
            {
                LevelId = levelId,
                Victory = victory,
                Distance = _currentDistance,
                SquadAtEnd = _currentSquad,
                MultiplierReached = _multiplierReached,
                KillsByArchetype = new Dictionary<string, int>(_killsByArchetype),
                Rescued = _rescuedSurvivorsCount,
                DurationSeconds = 0f,
                Stars = stars,
                HoldoutsAttempted = _holdoutsAttempted,
                HoldoutsHeld = _holdoutsHeld,
                MutationIds = _config != null && _config.MutationIds != null ? new List<string>(_config.MutationIds) : new List<string>()
            };

            OnRunEnded?.Invoke(result);
            _eventBus?.Publish(new RunEndedEvent(result));
        }

        public void RecordHoldoutStarted()
        {
            _holdoutsAttempted++;
        }

        public void RecordHoldoutEnded(bool barricadeHeld)
        {
            if (barricadeHeld)
            {
                _holdoutsHeld++;
            }
        }

        private void OnHoldoutStarted(HoldoutStartedEvent evt)
        {
            RecordHoldoutStarted();
        }

        private void OnHoldoutEnded(HoldoutEndedEvent evt)
        {
            RecordHoldoutEnded(evt.BarricadeHeld);
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

            if (IsBossFightActive && IsBossArchetype(evt.ArchetypeId))
            {
                ConcludeBossFight(evt.ArchetypeId);
            }
        }

        public void ConcludeBossFight(string bossId = null)
        {
            if (!IsBossFightActive)
            {
                return;
            }

            string concludedId = bossId ?? ActiveBossId;
            IsBossFightActive = false;
            BossCombatState?.EndArena();

            if (!string.IsNullOrEmpty(concludedId))
            {
                _eventBus?.Publish(new BossDefeatedEvent(concludedId, _currentDistance));
                OnBossFightEnded?.Invoke(concludedId);
            }
        }

        public void TickBossCombat(float deltaTime)
        {
            if (IsBossFightActive && BossCombatState != null && deltaTime > 0f)
            {
                BossCombatState.Tick(deltaTime);
            }
        }

        private void OnBossCombatStateDied()
        {
            if (IsBossFightActive)
            {
                ConcludeBossFight(ActiveBossId);
            }
        }

        private void OnBossDefeated(BossDefeatedEvent evt)
        {
            if (IsBossFightActive && string.Equals(evt.BossId, ActiveBossId, StringComparison.OrdinalIgnoreCase))
            {
                ConcludeBossFight(evt.BossId);
            }
        }

        private void OnMultiplierReached(MultiplierReachedEvent evt)
        {
            _multiplierReached = evt.Multiplier;
        }

        private void OnSurvivorsRescued(SurvivorsRescuedEvent evt)
        {
            _rescuedSurvivorsCount += evt.Count;
        }

        private void OnSurvivorsLost(SurvivorsLostEvent evt)
        {
            _pendingBonusZombiesByLane.TryGetValue(evt.LaneIndex, out int current);
            _pendingBonusZombiesByLane[evt.LaneIndex] = current + evt.Count;
        }

        private void CleanupSubscriptions()
        {
            _squadSub?.Dispose();
            _squadSub = null;
            _enemyKilledSub?.Dispose();
            _enemyKilledSub = null;
            _multiplierSub?.Dispose();
            _multiplierSub = null;
            _survivorsRescuedSub?.Dispose();
            _survivorsRescuedSub = null;
            _survivorsLostSub?.Dispose();
            _survivorsLostSub = null;
            _holdoutStartedSub?.Dispose();
            _holdoutStartedSub = null;
            _holdoutEndedSub?.Dispose();
            _holdoutEndedSub = null;
            _bossDefeatedSub?.Dispose();
            _bossDefeatedSub = null;
        }

        private void OnDestroy()
        {
            CleanupSubscriptions();
        }
    }
}
