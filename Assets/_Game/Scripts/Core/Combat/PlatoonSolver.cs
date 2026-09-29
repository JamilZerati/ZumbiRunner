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
            if (squadCount <= 0)
            {
                return Array.Empty<PlatoonEmitter>();
            }

            int emitterCount = Math.Min((squadCount + SoldiersPerEmitterThreshold - 1) / SoldiersPerEmitterThreshold, MaxEmitters);
            FormationPosition[] positions = FormationSolver.CalculatePositions(emitterCount, spacing, maxPerRow);

            int baseSoldiers = squadCount / emitterCount;
            int remainder = squadCount % emitterCount;

            var emitters = new PlatoonEmitter[emitterCount];

            for (int i = 0; i < emitterCount; i++)
            {
                int soldierCount = baseSoldiers + (i < remainder ? 1 : 0);
                emitters[i] = new PlatoonEmitter(i, soldierCount, positions[i]);
            }

            return emitters;
        }
    }
}
