using System.Collections.Generic;
using Game.Core.Status;

namespace Game.Core.Events
{
    public readonly struct EnemyKilledEvent
    {
        public string ArchetypeId { get; }
        public int Lane { get; }
        public IReadOnlyList<StatusKind> StatusesAtDeath { get; }
        public bool ByAbility { get; }

        public EnemyKilledEvent(string archetypeId, int lane, IReadOnlyList<StatusKind> statusesAtDeath = null, bool byAbility = false)
        {
            ArchetypeId = archetypeId;
            Lane = lane;
            StatusesAtDeath = statusesAtDeath ?? System.Array.Empty<StatusKind>();
            ByAbility = byAbility;
        }
    }
}
