using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;
using Game.Core.Status;
using UnityEngine;

namespace Game.Gameplay
{
    public class StatusEffectDirector : MonoBehaviour, IStatusNeighborhood
    {
        [SerializeField] private Game.Data.StatusCatalog catalog;
        [SerializeField] private Game.Data.EffectInteractionTable interactions;

        private IStatusCatalog _catalog;
        private IEffectInteractionTable _interactions;
        private IEventBus _eventBus;
        private readonly List<EnemyController> _registered = new List<EnemyController>();

        public IStatusCatalog Catalog => _catalog ?? catalog;
        public IEffectInteractionTable Interactions => _interactions ?? interactions;
        public IEventBus EventBus => _eventBus;
        public IReadOnlyList<EnemyController> Registered => _registered;

        public void Initialize(IStatusCatalog catalog, IEffectInteractionTable interactions, IEventBus eventBus = null)
        {
            _catalog = catalog;
            _interactions = interactions;
            _eventBus = eventBus;
        }

        public void Register(EnemyController enemy)
        {
            if (enemy == null || _registered.Contains(enemy))
            {
                return;
            }
            _registered.Add(enemy);
        }

        public void Unregister(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }
            _registered.Remove(enemy);
        }

        public IReadOnlyList<IStatusReceiver> FindNearby(IStatusReceiver origin, float radius, int maxCount)
        {
            if (maxCount <= 0 || radius <= 0f)
            {
                return Array.Empty<IStatusReceiver>();
            }

            Vector3 originPos = Vector3.zero;
            if (origin is Component originComp && originComp != null)
            {
                originPos = originComp.transform.position;
            }

            var candidates = new List<(EnemyController enemy, float distance, int order)>();
            float sqrRadius = radius * radius;

            for (int i = 0; i < _registered.Count; i++)
            {
                var candidate = _registered[i];
                if (candidate == null || ReferenceEquals(candidate, origin))
                {
                    continue;
                }

                if (origin is Component comp && comp != null && candidate.gameObject == comp.gameObject)
                {
                    continue;
                }

                if (!candidate.IsActiveInPool || !candidate.IsAlive)
                {
                    continue;
                }

                float sqrDist = (candidate.transform.position - originPos).sqrMagnitude;
                if (sqrDist <= sqrRadius + 1e-4f)
                {
                    candidates.Add((candidate, sqrDist, i));
                }
            }

            candidates.Sort((a, b) =>
            {
                float diff = a.distance - b.distance;
                if (Math.Abs(diff) > 1e-5f)
                {
                    return diff < 0 ? -1 : 1;
                }
                return a.order.CompareTo(b.order);
            });

            int count = Math.Min(candidates.Count, maxCount);
            var result = new IStatusReceiver[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = candidates[i].enemy;
            }
            return result;
        }
    }
}
