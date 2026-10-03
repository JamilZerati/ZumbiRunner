using System;
using System.Collections.Generic;
using Game.Core;
using Game.Core.Events;

namespace Game.Core.Status
{
    public sealed class StatusEffectController : IStatusHost
    {
        private readonly IDamageable health;
        private readonly IStatusReceiver owner;
        private readonly IStatusCatalog catalog;
        private readonly IEffectInteractionTable interactions;
        private readonly IStatusNeighborhood neighborhood;
        private readonly IEventBus eventBus;

        private readonly Dictionary<StatusKind, StatusState> states = new Dictionary<StatusKind, StatusState>();
        private readonly List<StatusKind> _lastActiveStatuses = new List<StatusKind>();
        private bool diedNotified;

        public StatusEffectController(IDamageable health, IStatusReceiver owner,
                                      IStatusCatalog catalog = null, IEffectInteractionTable interactions = null,
                                      IStatusNeighborhood neighborhood = null, IEventBus eventBus = null)
        {
            this.health = health ?? throw new ArgumentNullException(nameof(health));
            this.owner = owner;
            this.catalog = catalog;
            this.interactions = interactions;
            this.neighborhood = neighborhood;
            this.eventBus = eventBus;
        }

        public IStatusReceiver Owner => owner;
        public IStatusNeighborhood Neighborhood => neighborhood;
        public IStatusCatalog Catalog => catalog;

        public IReadOnlyList<StatusKind> ActiveStatuses
        {
            get
            {
                if (states.Count > 0)
                {
                    return new List<StatusKind>(states.Keys);
                }
                return _lastActiveStatuses.Count > 0 ? _lastActiveStatuses : Array.Empty<StatusKind>();
            }
        }

        public bool IsStunned => Has(StatusKind.Frozen);
        public bool IsFrozen => Has(StatusKind.Frozen);

        public float MoveSpeedMultiplier
        {
            get
            {
                if (Has(StatusKind.Frozen))
                {
                    return 0f;
                }

                if (states.TryGetValue(StatusKind.Slow, out var slowState))
                {
                    if (catalog != null && catalog.TryGet(StatusKind.Slow, out var slowEffect))
                    {
                        return slowEffect.MoveSpeedMultiplier(slowState);
                    }

                    return Math.Max(0f, 1f - slowState.Potency);
                }

                return 1f;
            }
        }

        public void Tick(float deltaTime)
        {
            if (states.Count == 0) return;
            if (!health.IsAlive || (owner != null && !owner.IsAlive)) return;

            var copy = new StatusState[states.Count];
            states.Values.CopyTo(copy, 0);

            for (int i = 0; i < copy.Length; i++)
            {
                var state = copy[i];
                if (catalog != null && catalog.TryGet(state.Kind, out var effect))
                {
                    effect.OnTick(this, state, deltaTime);
                }

                if (state.Remaining <= 0f)
                {
                    states.Remove(state.Kind);
                }

                if (!health.IsAlive || (owner != null && !owner.IsAlive))
                {
                    break;
                }
            }
        }

        public int ResolveHit(DamageInfo hit, IReadOnlyList<StatusApplication> onHit)
        {
            if (!health.IsAlive || (owner != null && !owner.IsAlive))
            {
                return 0;
            }

            int finalDamage = hit.Amount;

            if (interactions != null && interactions.Interactions != null)
            {
                var list = interactions.Interactions;
                for (int i = 0; i < list.Count; i++)
                {
                    var interaction = list[i];
                    if (interaction.Trigger == InteractionTrigger.HeavyHit &&
                        Has(interaction.RequiredStatus) &&
                        hit.Amount >= interaction.MinHitDamage)
                    {
                        finalDamage = (int)Math.Round((double)hit.Amount * interaction.DamageMultiplier, MidpointRounding.AwayFromZero);
                        Remove(interaction.RequiredStatus);
                        if (eventBus != null)
                        {
                            eventBus.Publish(new SynergyTriggeredEvent(interaction.Id, owner, hit.Amount, finalDamage));
                        }
                        break;
                    }
                }
            }

            health.TakeDamage(new DamageInfo(finalDamage, hit.Type, hit.Source));

            if (!health.IsAlive || (owner != null && !owner.IsAlive))
            {
                NotifyHostDied();
                return finalDamage;
            }

            if (onHit != null && onHit.Count > 0)
            {
                for (int i = 0; i < onHit.Count; i++)
                {
                    Apply(onHit[i]);
                }
            }

            return finalDamage;
        }

        public void Clear()
        {
            states.Clear();
            _lastActiveStatuses.Clear();
            diedNotified = false;
        }

        public bool Has(StatusKind kind) => states.ContainsKey(kind);

        public int GetStacks(StatusKind kind)
        {
            return states.TryGetValue(kind, out var state) ? state.Stacks : 0;
        }

        public void Apply(StatusApplication application)
        {
            if (!health.IsAlive || (owner != null && !owner.IsAlive))
            {
                return;
            }

            if (catalog == null || !catalog.TryGet(application.Kind, out var effect))
            {
                return;
            }

            if (effect.IsPersistent)
            {
                if (!states.TryGetValue(application.Kind, out var state))
                {
                    state = new StatusState(application.Kind);
                    states[application.Kind] = state;
                }

                effect.OnApply(this, state, application.Stacks);

                if (state.Stacks <= 0)
                {
                    states.Remove(application.Kind);
                }
            }
            else
            {
                effect.OnApply(this, null, application.Stacks);
            }
        }

        public bool Remove(StatusKind kind) => states.Remove(kind);

        public void DealDamage(int amount, DamageType type, object source)
        {
            health.TakeDamage(new DamageInfo(amount, type, source));
            if (!health.IsAlive || (owner != null && !owner.IsAlive))
            {
                NotifyHostDied();
            }
        }

        private void NotifyHostDied()
        {
            if (diedNotified) return;
            diedNotified = true;

            if (states.Count > 0)
            {
                _lastActiveStatuses.Clear();
                _lastActiveStatuses.AddRange(states.Keys);

                var copy = new StatusState[states.Count];
                states.Values.CopyTo(copy, 0);
                states.Clear();

                for (int i = 0; i < copy.Length; i++)
                {
                    var state = copy[i];
                    if (catalog != null && catalog.TryGet(state.Kind, out var effect))
                    {
                        effect.OnHostDied(this, state);
                    }
                }
            }
        }
    }
}
