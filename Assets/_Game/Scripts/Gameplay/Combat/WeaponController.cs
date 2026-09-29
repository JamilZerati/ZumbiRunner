using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Stats;
using Game.Core.Status;
using Game.Data;
using Game.Infrastructure;
using UnityEngine;

namespace Game.Gameplay
{
    public class WeaponController : MonoBehaviour, IWeaponLoadout
    {
        private readonly OnHitStatusSet onHitStatuses = new OnHitStatusSet();
        public OnHitStatusSet OnHitStatuses => onHitStatuses;

        // Iguais à pistol: a cena M4, montada sem catálogo, mantém o tiro que tinha antes dos stats.
        private static readonly WeaponProfile DefaultBaseProfile =
            new WeaponProfile(string.Empty, 2f, 2, 15f, 40f, 1, 0f);

        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private WeaponCatalog catalog;
        [SerializeField] private string initialWeaponId;

        // Criada no inicializador, não em Awake: cena reaberta em EditMode e testes usam o controller sem Awake.
        private readonly StatCollection stats = CreateDefaultStats();
        private float spreadWidth = DefaultBaseProfile.SpreadWidth;
        private string equippedWeaponId = string.Empty;
        private IWeaponCatalog injectedCatalog;
        private IEventBus eventBus;

        public float FireRate
        {
            get => CurrentStats.FireRate;
            set => stats.SetBase(StatId.FireRate, value);
        }

        public int DamagePerShot
        {
            get => CurrentStats.Damage;
            set => stats.SetBase(StatId.Damage, value);
        }

        public float ProjectileSpeed
        {
            get => CurrentStats.ProjectileSpeed;
            set => stats.SetBase(StatId.ProjectileSpeed, value);
        }

        public float MaxDistance
        {
            get => CurrentStats.Range;
            set => stats.SetBase(StatId.Range, value);
        }

        public bool IsFiring { get; set; } = true;
        public ISquad Squad { get; private set; }

        private IObjectPool<Projectile> _pool;
        private bool _isPoolExplicitlySet;

        public IObjectPool<Projectile> Pool
        {
            get
            {
                if (!_isPoolExplicitlySet && _pool == null && projectilePrefab != null)
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

        public Projectile ProjectilePrefab
        {
            get => projectilePrefab;
            set => projectilePrefab = value;
        }

        public float FireTimer { get; private set; }

        private Action<Projectile> _onProjectileRecycle;

        public StatCollection Stats => stats;
        public string EquippedWeaponId => equippedWeaponId;
        public WeaponStats CurrentStats => WeaponStats.Resolve(stats, spreadWidth);

        private IWeaponCatalog EffectiveCatalog
        {
            get
            {
                if (injectedCatalog != null)
                {
                    return injectedCatalog;
                }

                return catalog != null ? catalog : null;
            }
        }

        public void Initialize(IObjectPool<Projectile> pool, ISquad squad = null,
                               IWeaponCatalog catalog = null, IEventBus eventBus = null)
        {
            Pool = pool;
            Squad = squad;
            FireTimer = 0f;
            _onProjectileRecycle = p => Pool?.Return(p);
            injectedCatalog = catalog;
            this.eventBus = eventBus;
        }

        public bool TryEquip(string weaponId)
        {
            var weaponCatalog = EffectiveCatalog;
            if (weaponCatalog == null || !weaponCatalog.TryGet(weaponId, out WeaponProfile profile))
            {
                return false;
            }

            string previousWeaponId = equippedWeaponId;
            profile.ApplyAsBase(stats);
            spreadWidth = profile.SpreadWidth;
            equippedWeaponId = profile.Id;
            ClampFireTimerToTwoIntervals(CurrentStats.FireRate);
            eventBus?.Publish(new WeaponEquippedEvent(equippedWeaponId, previousWeaponId));
            return true;
        }

        public IReadOnlyList<Projectile> FireVolley()
        {
            if (Pool == null)
            {
                return Array.Empty<Projectile>();
            }

            var current = CurrentStats;
            int count = current.ProjectileCount;
            var volley = new List<Projectile>(count);
            Vector3 origin = transform.position + Vector3.forward * 0.5f;
            var onRecycle = _onProjectileRecycle ?? (p => Pool.Return(p));
            var onHitSnapshot = onHitStatuses.Snapshot();

            for (int i = 0; i < count; i++)
            {
                var proj = Pool.Rent();
                if (proj == null)
                {
                    continue;
                }

                proj.transform.position = origin + Vector3.right * LateralOffset(i, count, current.SpreadWidth);
                proj.gameObject.SetActive(true);
                proj.Initialize(current.Damage, current.ProjectileSpeed, current.Range, onRecycle, onHit: onHitSnapshot);
                volley.Add(proj);
            }

            return volley;
        }

        // Com um projétil só, w/(N-1) divide por zero e a posição vira NaN sem erro.
        private static float LateralOffset(int index, int count, float width)
        {
            return count > 1 ? -width / 2f + index * width / (count - 1) : 0f;
        }

        private void ClampFireTimerToTwoIntervals(float fireRate)
        {
            if (fireRate <= 0f)
            {
                return;
            }

            float maxTimer = 2f / fireRate;
            if (FireTimer > maxTimer)
            {
                FireTimer = maxTimer;
            }
        }

        private static StatCollection CreateDefaultStats()
        {
            var defaultStats = new StatCollection();
            DefaultBaseProfile.ApplyAsBase(defaultStats);
            return defaultStats;
        }

        private void Start()
        {
            if (Pool == null)
            {
                EnsurePoolInitialized();
            }

            if (!string.IsNullOrEmpty(initialWeaponId) && string.IsNullOrEmpty(equippedWeaponId))
            {
                TryEquip(initialWeaponId);
            }
        }

        public void EnsurePoolInitialized()
        {
            if (_pool != null)
            {
                return;
            }

            if (projectilePrefab == null)
            {
                projectilePrefab = GetComponentInChildren<Projectile>(true)
                    ?? FindFirstObjectByType<Projectile>(FindObjectsInactive.Include);
            }

            if (projectilePrefab == null)
            {
                return;
            }

            var poolGo = new GameObject("ProjectilePool");
            poolGo.transform.SetParent(transform, false);

            var pool = new ObjectPool<Projectile>(
                factory: () => Instantiate(projectilePrefab, poolGo.transform),
                onRent: p => p.gameObject.SetActive(true),
                onReturn: p => p.gameObject.SetActive(false),
                initialCapacity: 10
            );

            Initialize(pool, Squad ?? GetComponent<SquadController>() ?? GetComponentInParent<SquadController>(),
                       injectedCatalog, eventBus);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            float fireRate = CurrentStats.FireRate;
            if (!IsFiring || Pool == null || fireRate <= 0f)
            {
                return;
            }

            FireTimer += deltaTime;
            float interval = 1f / fireRate;
            if (FireTimer > interval * 2f)
            {
                FireTimer = interval * 2f;
            }

            if (FireTimer >= interval)
            {
                FireTimer -= interval;
                Fire();
            }
        }

        public Projectile Fire()
        {
            var volley = FireVolley();
            return volley.Count > 0 ? volley[0] : null;
        }
    }
}
