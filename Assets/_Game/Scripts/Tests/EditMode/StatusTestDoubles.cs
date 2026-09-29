using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Status;

namespace Game.Tests.EditMode
{
    public class FakeHealth : IDamageable
    {
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public bool IsAlive => CurrentHealth > 0;

        public FakeHealth(int maxHealth = 40)
        {
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(DamageInfo damage)
        {
            CurrentHealth = Math.Max(0, CurrentHealth - damage.Amount);
        }
    }

    public class FakeReceiver : IStatusReceiver
    {
        public FakeHealth Health { get; }
        public StatusEffectController Controller { get; set; }
        public bool IsAlive => Health.IsAlive;
        public List<DamageInfo> ReceivedDamage { get; } = new List<DamageInfo>();
        public List<IReadOnlyList<StatusApplication>> ReceivedOnHit { get; } = new List<IReadOnlyList<StatusApplication>>();

        public FakeReceiver(int health = 40)
        {
            Health = new FakeHealth(health);
        }

        public int ReceiveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit)
        {
            ReceivedDamage.Add(hit);
            ReceivedOnHit.Add(onHit);
            if (Controller != null)
            {
                return Controller.ResolveHit(hit, onHit);
            }

            Health.TakeDamage(hit);
            return hit.Amount;
        }
    }

    public class FakeNeighborhood : IStatusNeighborhood
    {
        private class Entry
        {
            public IStatusReceiver Receiver;
            public float X;
            public int Order;
        }

        private readonly List<Entry> entries = new List<Entry>();

        public void Register(IStatusReceiver receiver, float x)
        {
            entries.Add(new Entry { Receiver = receiver, X = x, Order = entries.Count });
        }

        public IReadOnlyList<IStatusReceiver> FindNearby(IStatusReceiver origin, float radius, int maxCount)
        {
            float originX = 0f;
            foreach (var e in entries)
            {
                if (e.Receiver == origin)
                {
                    originX = e.X;
                    break;
                }
            }

            var candidates = new List<(IStatusReceiver receiver, float dist, int order)>();
            foreach (var e in entries)
            {
                if (e.Receiver == origin || !e.Receiver.IsAlive)
                {
                    continue;
                }

                float dist = Math.Abs(e.X - originX);
                if (dist <= radius)
                {
                    candidates.Add((e.Receiver, dist, e.Order));
                }
            }

            candidates.Sort((a, b) =>
            {
                int cmp = a.dist.CompareTo(b.dist);
                if (cmp != 0) return cmp;
                return a.order.CompareTo(b.order);
            });

            int count = Math.Min(maxCount, candidates.Count);
            var result = new List<IStatusReceiver>(count);
            for (int i = 0; i < count; i++)
            {
                result.Add(candidates[i].receiver);
            }
            return result;
        }
    }

    public class InMemoryStatusCatalog : IStatusCatalog
    {
        private readonly Dictionary<StatusKind, IStatusEffect> effects = new Dictionary<StatusKind, IStatusEffect>();

        public void Register(IStatusEffect effect)
        {
            effects[effect.Kind] = effect;
        }

        public bool TryGet(StatusKind kind, out IStatusEffect effect)
        {
            return effects.TryGetValue(kind, out effect);
        }

        public static InMemoryStatusCatalog CreateDefault()
        {
            var catalog = new InMemoryStatusCatalog();
            catalog.Register(new BurnStatus { Duration = 3f, TickInterval = 0.5f, DamagePerTick = 3 });
            catalog.Register(new FreezeStatus { Duration = 3f, Threshold = 2 });
            catalog.Register(new FrozenStatus { Duration = 1.5f });
            catalog.Register(new SlowStatus { Duration = 2f, SlowPercent = 0.4f });
            catalog.Register(new ShockStatus { ChainCount = 2, ChainRadius = 4f, ChainDamage = 6 });
            catalog.Register(new PoisonStatus
            {
                Duration = 4f,
                TickInterval = 1f,
                DamagePerTickPerStack = 1,
                MaxStacks = 5,
                ExplosionDamagePerStack = 4,
                ExplosionRadius = 2.5f
            });
            return catalog;
        }
    }

    public class InMemoryInteractionTable : IEffectInteractionTable
    {
        public List<EffectInteraction> List { get; } = new List<EffectInteraction>();
        public IReadOnlyList<EffectInteraction> Interactions => List;

        public static InMemoryInteractionTable CreateDefault()
        {
            var table = new InMemoryInteractionTable();
            table.List.Add(new EffectInteraction("shatter", StatusKind.Frozen, InteractionTrigger.HeavyHit, 12, 2f));
            return table;
        }
    }
}
