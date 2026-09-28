using System;
using Game.Core;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Gameplay
{
    public class WeaponController : MonoBehaviour
    {
        public float FireRate { get; set; } = 2f;
        public int DamagePerShot { get; set; } = 10;
        public float ProjectileSpeed { get; set; } = 15f;
        public float MaxDistance { get; set; } = 40f;
        public bool IsFiring { get; set; } = true;
        public ISquad Squad { get; private set; }
        public IObjectPool<Projectile> Pool { get; private set; }
        public float FireTimer { get; private set; }

        public void Initialize(IObjectPool<Projectile> pool, ISquad squad = null)
        {
            Pool = pool;
            Squad = squad;
            FireTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!IsFiring || Pool == null || FireRate <= 0f)
            {
                return;
            }

            FireTimer += deltaTime;
            float interval = 1f / FireRate;
            if (FireTimer >= interval)
            {
                FireTimer -= interval;
                Fire();
            }
        }

        public Projectile Fire()
        {
            if (Pool == null)
            {
                return null;
            }

            var proj = Pool.Rent();
            if (proj == null)
            {
                return null;
            }

            proj.transform.position = transform.position + Vector3.forward * 0.5f;
            proj.gameObject.SetActive(true);
            proj.Initialize(DamagePerShot, ProjectileSpeed, MaxDistance, p => Pool.Return(p));
            return proj;
        }
    }
}
