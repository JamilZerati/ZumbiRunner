using System;
using Game.Core.Status;

namespace Game.Core.Perks.Effects
{
    [Serializable]
    public class ApplyStatusOnHitEffect : IPerkEffect
    {
        public StatusKind Status;
        public int Stacks;

        public string Description => throw new NotImplementedException();
        public void Apply(PerkContext context) => throw new NotImplementedException();
    }
}
