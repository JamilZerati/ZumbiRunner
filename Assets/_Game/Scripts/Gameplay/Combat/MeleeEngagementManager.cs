using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay
{
    public class MeleeEngagementManager : MonoBehaviour
    {
        [SerializeField] private float soldierMaxHealth = 10f;
        [SerializeField] private float generalMaxHealth = 10f;
        [SerializeField] private float forwardEngagementOffset = 0.8f;

        private SquadController _squad;
        private CombatDirector _combatDirector;
        private IEventBus _eventBus;
        private SquadHealthBuffer _healthBuffer;
        private readonly List<EnemyController> _engagedEnemies = new List<EnemyController>();

        public IReadOnlyList<EnemyController> EngagedEnemies => _engagedEnemies;

        public SquadHealthBuffer HealthBuffer
        {
            get
            {
                if (_healthBuffer == null)
                {
                    _healthBuffer = new SquadHealthBuffer(soldierMaxHealth, generalMaxHealth);
                }
                return _healthBuffer;
            }
        }

        public float ForwardEngagementOffset
        {
            get => forwardEngagementOffset;
            set => forwardEngagementOffset = value;
        }

        public float SoldierMaxHealth
        {
            get => soldierMaxHealth;
            set => soldierMaxHealth = value;
        }

        public float GeneralMaxHealth
        {
            get => generalMaxHealth;
            set => generalMaxHealth = value;
        }

        public void Initialize(SquadController squad, CombatDirector combatDirector, IEventBus eventBus = null)
        {
            _squad = squad;
            _combatDirector = combatDirector;
            if (_combatDirector != null && _combatDirector.MeleeManager == null)
            {
                _combatDirector.SetMeleeManager(this);
            }
            _eventBus = eventBus;
            _healthBuffer = new SquadHealthBuffer(soldierMaxHealth, generalMaxHealth);
            _engagedEnemies.Clear();
        }

        private void Awake()
        {
            if (_healthBuffer == null)
            {
                _healthBuffer = new SquadHealthBuffer(soldierMaxHealth, generalMaxHealth);
            }
            if (_squad == null)
            {
                _squad = GetComponent<SquadController>() ?? GetComponentInParent<SquadController>();
            }
            if (_combatDirector == null)
            {
                _combatDirector = GetComponent<CombatDirector>() ?? GetComponentInParent<CombatDirector>();
            }
            if (_combatDirector != null && _combatDirector.MeleeManager == null)
            {
                _combatDirector.SetMeleeManager(this);
            }
        }

        public bool Engage(EnemyController enemy)
        {
            if (enemy == null || !enemy.IsActiveInPool || !enemy.IsAlive || enemy.IsEngaged)
            {
                return false;
            }

            if (_engagedEnemies.Contains(enemy))
            {
                return false;
            }

            _engagedEnemies.Add(enemy);
            float lateralOffset = enemy.transform.position.x - transform.position.x;
            Vector3 offset = new Vector3(lateralOffset, 0f, forwardEngagementOffset);
            enemy.Engage(transform, offset);

            _eventBus?.Publish(new EnemyEngagedEvent(enemy.ArchetypeId, enemy.LaneIndex));
            return true;
        }

        public bool Disengage(EnemyController enemy, bool wasKilled = false)
        {
            if (enemy == null || !_engagedEnemies.Remove(enemy))
            {
                return false;
            }

            enemy.Disengage();
            _eventBus?.Publish(new EnemyDisengagedEvent(enemy.ArchetypeId, wasKilled));
            return true;
        }

        private SquadController Squad
        {
            get
            {
                if (_squad == null)
                {
                    _squad = GetComponent<SquadController>() ?? GetComponentInParent<SquadController>();
                }
                return _squad;
            }
        }

        private CombatDirector Director
        {
            get
            {
                if (_combatDirector == null)
                {
                    _combatDirector = GetComponent<CombatDirector>() ?? GetComponentInParent<CombatDirector>();
                }
                return _combatDirector;
            }
        }

        public void Tick(float deltaTime)
        {
            if (Director != null && Director.IsResolved)
            {
                ClearAll();
                return;
            }

            for (int i = _engagedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _engagedEnemies[i];
                if (enemy == null || !enemy.IsActiveInPool || !enemy.IsAlive || !enemy.IsEngaged)
                {
                    _engagedEnemies.RemoveAt(i);
                    if (enemy != null)
                    {
                        enemy.Disengage();
                    }
                    _eventBus?.Publish(new EnemyDisengagedEvent(enemy?.ArchetypeId ?? "walker", true));
                }
            }

            if (_engagedEnemies.Count == 0)
            {
                return;
            }

            float totalDamage = 0f;
            for (int i = 0; i < _engagedEnemies.Count; i++)
            {
                var enemy = _engagedEnemies[i];
                if (enemy != null && enemy.IsAlive && enemy.IsActiveInPool)
                {
                    if (enemy.Status != null && enemy.Status.IsFrozen)
                    {
                        continue;
                    }
                    totalDamage += enemy.ContactDPS * deltaTime;
                }
            }

            if (totalDamage <= 0f)
            {
                return;
            }

            int currentSquad = Squad != null ? Squad.SquadCount : 0;
            int soldiersLost = HealthBuffer.ApplyDamage(totalDamage, currentSquad, out bool generalDied);

            if (soldiersLost > 0 && Squad != null)
            {
                Squad.Remove(soldiersLost);
            }

            _eventBus?.Publish(new SoldierDamagedEvent(HealthBuffer.CurrentSoldierHealth, HealthBuffer.SoldierMaxHealth));

            if (generalDied && Director != null)
            {
                Director.TriggerDefeat();
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void ClearAll()
        {
            for (int i = 0; i < _engagedEnemies.Count; i++)
            {
                if (_engagedEnemies[i] != null)
                {
                    _engagedEnemies[i].Disengage();
                }
            }
            _engagedEnemies.Clear();
        }
    }
}
