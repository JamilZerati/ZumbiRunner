using System;

namespace Game.Core
{
    public static class PlatoonSolver
    {
        public const int MaxEmitters = 40;
        public const int SoldiersPerEmitterThreshold = 5;

        public static PlatoonEmitter[] CalculatePlatoons(
            int squadCount,
            float spacing = FormationSolver.DefaultSpacing,
            int maxPerRow = FormationSolver.DefaultMaxPerRow)
        {
            throw new NotImplementedException("PlatoonSolver will be implemented in Marco 1 [NEX-666].");
        }
    }
}
