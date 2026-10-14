using System;
using System.Collections.Generic;

namespace Game.Core.Allies
{
    public class AllyRoster : IAllyRoster
    {
        private readonly List<IAllyInstance> _allies = new List<IAllyInstance>();
        private readonly Func<string, IAllyInstance> _factory;

        public IReadOnlyList<IAllyInstance> Allies => _allies;

        public int HeroCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _allies.Count; i++)
                {
                    if (!_allies[i].IsPet) count++;
                }
                return count;
            }
        }

        public int PetCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _allies.Count; i++)
                {
                    if (_allies[i].IsPet) count++;
                }
                return count;
            }
        }

        public AllyRoster(Func<string, IAllyInstance> factory = null)
        {
            _factory = factory;
        }

        public bool TryAddAlly(IAllyInstance ally, out IAllyInstance registeredAlly)
        {
            if (ally == null || string.IsNullOrEmpty(ally.Id))
            {
                registeredAlly = null;
                return false;
            }

            if (TryGetAlly(ally.Id, out var existing))
            {
                existing.RestoreFullHealth();
                registeredAlly = existing;
                return false;
            }

            _allies.Add(ally);
            registeredAlly = ally;
            return true;
        }

        public bool TryRecruit(string allyId, out IAllyInstance registeredAlly)
        {
            if (string.IsNullOrEmpty(allyId))
            {
                registeredAlly = null;
                return false;
            }

            if (TryGetAlly(allyId, out var existing))
            {
                existing.RestoreFullHealth();
                registeredAlly = existing;
                return false;
            }

            var instance = _factory != null ? _factory(allyId) : CreateDefaultInstance(allyId);
            _allies.Add(instance);
            registeredAlly = instance;
            return true;
        }

        public bool TryGetAlly(string id, out IAllyInstance ally)
        {
            ally = null;
            if (string.IsNullOrEmpty(id)) return false;

            for (int i = 0; i < _allies.Count; i++)
            {
                if (string.Equals(_allies[i].Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    ally = _allies[i];
                    return true;
                }
            }

            return false;
        }

        public bool ContainsAlly(string id) => TryGetAlly(id, out _);

        public void Clear()
        {
            _allies.Clear();
        }

        private static IAllyInstance CreateDefaultInstance(string id)
        {
            if (string.Equals(id, "pyromancer", StringComparison.OrdinalIgnoreCase))
            {
                return new AllyInstance("pyromancer", "Piromante", AllyArchetype.Hero, 150,
                    new AbilityRunner("Inferno", 12f, 0f));
            }

            if (string.Equals(id, "cryomancer", StringComparison.OrdinalIgnoreCase))
            {
                return new AllyInstance("cryomancer", "Criomante", AllyArchetype.Hero, 120,
                    new AbilityRunner("Blizzard", 14f, 0f));
            }

            if (string.Equals(id, "guard_dog", StringComparison.OrdinalIgnoreCase))
            {
                return new AllyInstance("guard_dog", "Cão de Guarda", AllyArchetype.Pet, 100);
            }

            return new AllyInstance(id, id, AllyArchetype.Hero, 100);
        }
    }
}
