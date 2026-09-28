using System;
using System.Collections.Generic;
using Game.Core;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Gameplay
{
    public class HordeSpawner : MonoBehaviour
    {
        public LaneLayout Layout { get; private set; }
        public IObjectPool<EnemyController> Pool { get; private set; }
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
