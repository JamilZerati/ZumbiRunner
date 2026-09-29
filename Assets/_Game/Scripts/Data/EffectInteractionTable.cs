using System;
using System.Collections.Generic;
using Game.Core.Status;
using UnityEngine;

namespace Game.Data
{
    public class EffectInteractionTable : ScriptableObject, IEffectInteractionTable
    {
        public IReadOnlyList<EffectInteraction> Interactions => throw new NotImplementedException();
        public void SetInteractions(IEnumerable<EffectInteraction> interactions) => throw new NotImplementedException();
    }
}
