using UnityEngine;
using Game.Core;

namespace Game.Gameplay
{
    public class CombatDirector : MonoBehaviour
    {
        [SerializeField] private SquadController squad;
        [SerializeField] private TrackScroller scroller;
        [SerializeField] private float victoryDistance = 120f;
        [SerializeField] private HordeSpawner spawner;

        public SquadController Squad { get; private set; }
        public TrackScroller Scroller { get; private set; }
        public IGameStateMachine StateMachine { get; private set; }
        public float VictoryDistance { get; set; } = 120f;
        public HordeSpawner Spawner { get; private set; }
        public bool IsResolved { get; private set; }

        public void Initialize(SquadController squad, TrackScroller scroller, IGameStateMachine stateMachine, float victoryDistance, HordeSpawner spawner = null)
        {
            Squad = squad;
            this.squad = squad;
            Scroller = scroller;
            this.scroller = scroller;
            StateMachine = stateMachine;
            VictoryDistance = victoryDistance;
            this.victoryDistance = victoryDistance;
            Spawner = spawner;
            this.spawner = spawner;
            IsResolved = false;
        }

        private void Awake()
        {
            if (Squad == null && squad != null)
            {
                Squad = squad;
            }

            if (Scroller == null && scroller != null)
            {
                Scroller = scroller;
            }

            if (Spawner == null && spawner != null)
            {
                Spawner = spawner;
            }

            if (VictoryDistance <= 0f)
            {
                VictoryDistance = victoryDistance > 0f ? victoryDistance : 120f;
            }
        }

        private void Start()
        {
            if (Squad == null)
            {
                Squad = GetComponent<SquadController>() ?? GetComponentInParent<SquadController>();
            }

            if (Scroller == null)
            {
                Scroller = GetComponent<TrackScroller>() ?? GetComponentInParent<TrackScroller>();
            }

            if (Spawner == null)
            {
                Spawner = FindFirstObjectByType<HordeSpawner>();
            }

            if (StateMachine == null)
            {
                var sm = new GameStateMachine(null, GameState.Boot);
                sm.TryTransition(GameState.Run);
                StateMachine = sm;
            }
        }

        public bool ResolveEnemyContact(EnemyController enemy)
        {
            if (IsResolved || enemy == null || !enemy.IsActiveInPool)
            {
                return false;
            }

            if (Squad != null && Squad.SquadCount > 0)
            {
                Squad.Remove(1);
                enemy.Recycle();
                return true;
            }

            enemy.Recycle();
            TriggerDefeat();
            return true;
        }

        public void TriggerDefeat()
        {
            if (IsResolved)
            {
                return;
            }

            IsResolved = true;

            if (Scroller != null)
            {
                Scroller.IsPaused = true;
            }

            StateMachine?.TryTransition(GameState.Defeat);
        }

        public bool CheckVictory()
        {
            if (IsResolved || StateMachine == null || StateMachine.CurrentState != GameState.Run)
            {
                return false;
            }

            if (Scroller != null && Scroller.DistanceTraveled >= VictoryDistance)
            {
                TriggerVictory();
                return true;
            }

            return false;
        }

        public void TriggerVictory()
        {
            if (IsResolved)
            {
                return;
            }

            IsResolved = true;

            if (Scroller != null)
            {
                Scroller.IsPaused = true;
            }

            StateMachine?.TryTransition(GameState.Victory);
        }

        public void Tick(float deltaTime)
        {
            if (!IsResolved && StateMachine?.CurrentState == GameState.Run)
            {
                CheckVictory();
            }
        }

        private void Update() => Tick(Time.deltaTime);

        public void OnTriggerEnter(Collider other)
        {
            if (other == null)
            {
                return;
            }

            var enemy = other.GetComponent<EnemyController>()
                ?? other.GetComponentInParent<EnemyController>()
                ?? (other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<EnemyController>() : null);

            if (enemy != null)
            {
                ResolveEnemyContact(enemy);
            }
        }
    }
}
