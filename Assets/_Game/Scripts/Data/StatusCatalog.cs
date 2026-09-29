using System;
using System.Collections.Generic;
using Game.Core.Status;
using UnityEngine;

namespace Game.Data
{
    public class StatusCatalog : ScriptableObject, IStatusCatalog
    {
        public IReadOnlyList<IStatusEffect> Effects => throw new NotImplementedException();
        public bool TryGet(StatusKind kind, out IStatusEffect effect) => throw new NotImplementedException();
        public void SetEffects(IEnumerable<IStatusEffect> effects) => throw new NotImplementedException();
    }
}
