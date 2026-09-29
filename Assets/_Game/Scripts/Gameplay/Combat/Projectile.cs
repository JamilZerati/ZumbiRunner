using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Status;
using UnityEngine;

namespace Game.Gameplay
{
    public class Projectile : MonoBehaviour
    {
        public int LaneIndex { get; set; }
        public float Speed { get; private set; }
        public int Damage { get; private set; }
        public float MaxDistance { get; private set; }
        public float TraveledDistance { get; private set; }
        public bool IsActiveInPool { get; private set; }
        public IReadOnlyList<StatusApplication> OnHit => throw new NotImplementedException();

        private Action<Projectile> _onRecycle;
        private bool _hasHit;

        public void Initialize(int damage, float speed, float maxDistance, Action<Projectile> onRecycle, int laneIndex = 0, IReadOnlyList<StatusApplication> onHit = null)
        {
            if (onHit != null)
            {
                throw new NotImplementedException();
            }
            Damage = damage;
            Speed = speed;
            MaxDistance = maxDistance;
            _onRecycle = onRecycle;
            LaneIndex = laneIndex;
            TraveledDistance = 0f;
            _hasHit = false;
            IsActiveInPool = true;

            if (TryGetComponent<Collider>(out var col))
            {
                col.enabled = true;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            if (!IsActiveInPool || !gameObject.activeSelf)
            {
                return;
            }

            float moveDistance = Speed * deltaTime;
            transform.position += Vector3.forward * moveDistance;
            TraveledDistance += moveDistance;

            if (TraveledDistance >= MaxDistance)
            {
                Recycle();
            }
        }

        public void Recycle()
        {
            if (!IsActiveInPool)
            {
                return;
            }

            IsActiveInPool = false;
            gameObject.SetActive(false);
            _onRecycle?.Invoke(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other != null)
            {
                HandleTrigger(other.gameObject);
            }
        }

        public void HandleTrigger(GameObject targetObject)
        {
            if (targetObject == null || !IsActiveInPool || _hasHit)
            {
                return;
            }

            var damageable = targetObject.GetComponent<IDamageable>() ?? targetObject.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                _hasHit = true;
                if (TryGetComponent<Collider>(out var col))
                {
                    col.enabled = false;
                }

                damageable.TakeDamage(new DamageInfo(Damage, DamageType.Physical, this));
                Recycle();
            }
        }
    }
}
