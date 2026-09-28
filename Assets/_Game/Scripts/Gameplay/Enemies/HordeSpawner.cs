using System;
using System.Collections.Generic;
using Game.Core;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Gameplay
{
    public class HordeSpawner : MonoBehaviour
    {
        [SerializeField] private int laneCount = 2;
        [SerializeField] private float laneWidth = 2.0f;
        [SerializeField] private EnemyController enemyPrefab;

        private LaneLayout? _layout;
        public LaneLayout Layout
        {
            get
            {
                if (!_layout.HasValue)
                {
                    _layout = new LaneLayout(laneCount > 0 ? laneCount : 2, laneWidth > 0 ? laneWidth : 2.0f);
                }
                return _layout.Value;
            }
            private set => _layout = value;
        }

        private IObjectPool<EnemyController> _pool;
        private bool _isPoolExplicitlySet;

        public IObjectPool<EnemyController> Pool
        {
            get
            {
                if (!_isPoolExplicitlySet && _pool == null && enemyPrefab != null)
                {
                    EnsurePoolInitialized();
                }
                return _pool;
            }
            private set
            {
                _pool = value;
                _isPoolExplicitlySet = true;
            }
        }

        public EnemyController EnemyPrefab
        {
            get => enemyPrefab;
            set => enemyPrefab = value;
        }

        public int DefaultEnemyHealth { get; set; } = 20;
        public float DefaultEnemySpeed { get; set; } = 2f;
        public IReadOnlyList<EnemyController> ActiveEnemies => _activeEnemies;

        private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
        private Action<EnemyController> _onEnemyRecycled;

        public void Initialize(LaneLayout layout, IObjectPool<EnemyController> pool)
        {
            Layout = layout;
            Pool = pool;
            _onEnemyRecycled = OnEnemyRecycled;
        }

        public void EnsurePoolInitialized()
        {
            if (_pool != null)
            {
                return;
            }

            if (enemyPrefab == null)
            {
                enemyPrefab = GetComponentInChildren<EnemyController>(true)
                    ?? FindFirstObjectByType<EnemyController>(FindObjectsInactive.Include);
            }

            if (enemyPrefab == null)
            {
                return;
            }

            var poolGo = new GameObject("EnemyPool");
            poolGo.transform.SetParent(transform, false);

            _pool = new ObjectPool<EnemyController>(
                factory: () => Instantiate(enemyPrefab, poolGo.transform),
                onRent: e => e.gameObject.SetActive(true),
                onReturn: e => e.gameObject.SetActive(false),
                initialCapacity: 15
            );
            _onEnemyRecycled = OnEnemyRecycled;
        }

        public List<EnemyController> SpawnWave(int laneIndex, int count, float startZ, float spacing = 1.5f)
        {
            if (!Layout.IsValidLane(laneIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(laneIndex), $"Invalid lane index: {laneIndex}");
            }

            if (count <= 0 || Pool == null)
            {
                return new List<EnemyController>();
            }

            var spawned = new List<EnemyController>(count);
            float x = Layout.GetLaneCenterX(laneIndex);
            var recycleCallback = _onEnemyRecycled ?? OnEnemyRecycled;

            for (int i = 0; i < count; i++)
            {
                var enemy = Pool.Rent();
                if (enemy == null)
                {
                    continue;
                }

                float z = startZ + (i * spacing);
                enemy.transform.position = new Vector3(x, 0f, z);
                enemy.gameObject.SetActive(true);
                enemy.Initialize(laneIndex, DefaultEnemyHealth, DefaultEnemySpeed, recycleCallback);

                _activeEnemies.Add(enemy);
                spawned.Add(enemy);
            }

            return spawned;
        }

        private void OnEnemyRecycled(EnemyController enemy)
        {
            _activeEnemies.Remove(enemy);
            Pool?.Return(enemy);
        }

        public void ClearActiveEnemies()
        {
            var enemiesToRecycle = _activeEnemies.ToArray();
            for (int i = 0; i < enemiesToRecycle.Length; i++)
            {
                enemiesToRecycle[i].Recycle();
            }

            _activeEnemies.Clear();
        }
    }
}
