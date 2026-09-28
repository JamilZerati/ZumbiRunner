using System;
using System.Collections.Generic;

namespace Game.Core.Stats
{
    public sealed class StatCollection
    {
#pragma warning disable CS0067 // Stub do Marco 0: o evento passa a ser disparado no Marco 1 (NEX-568).
        public event Action<StatId> Changed;
#pragma warning restore CS0067

        public float GetBase(StatId stat) => throw new NotImplementedException();

        public void SetBase(StatId stat, float value) => throw new NotImplementedException();

        public float GetValue(StatId stat) => throw new NotImplementedException();

        public void AddModifier(StatModifier modifier) => throw new NotImplementedException();

        public bool RemoveModifier(StatModifier modifier) => throw new NotImplementedException();

        public int RemoveAllFromSource(object source) => throw new NotImplementedException();

        public IReadOnlyList<StatModifier> GetModifiers(StatId stat) => throw new NotImplementedException();
    }
}
