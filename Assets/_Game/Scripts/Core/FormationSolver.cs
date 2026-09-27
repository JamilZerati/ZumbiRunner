using System;

namespace Game.Core
{
    public static class FormationSolver
    {
        public const float DefaultSpacing = 0.5f;
        public const int DefaultMaxPerRow = 5;

        public static FormationPosition[] CalculatePositions(int count, float spacing = DefaultSpacing, int maxPerRow = DefaultMaxPerRow)
        {
            if (count <= 0)
            {
                return Array.Empty<FormationPosition>();
            }

            if (spacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(spacing), "Spacing must be greater than zero.");
            }

            if (maxPerRow <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxPerRow), "Max per row must be greater than zero.");
            }

            var positions = new FormationPosition[count];

            for (int i = 0; i < count; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;
                int rowUnits = Math.Min(maxPerRow, count - (row * maxPerRow));

                float x = (col - ((rowUnits - 1) * 0.5f)) * spacing;
                float z = -(row + 1) * spacing;

                positions[i] = new FormationPosition(x, z);
            }

            return positions;
        }
    }
}
