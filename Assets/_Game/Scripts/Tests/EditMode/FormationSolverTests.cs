using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class FormationSolverTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void FormationPosition_PropertiesAndEquality()
        {
            var posA = new FormationPosition(1.5f, -2.5f);
            var posB = new FormationPosition(1.5f, -2.5f);
            var posC = new FormationPosition(0.0f, -2.5f);

            Assert.AreEqual(1.5f, posA.X, Tolerance);
            Assert.AreEqual(-2.5f, posA.Z, Tolerance);

            Assert.IsTrue(posA.Equals(posB));
            Assert.IsTrue(posA.Equals((object)posB));
            Assert.IsTrue(posA == posB);
            Assert.IsFalse(posA != posB);
            Assert.AreEqual(posA.GetHashCode(), posB.GetHashCode());

            Assert.IsFalse(posA.Equals(posC));
            Assert.IsFalse(posA == posC);
            Assert.IsTrue(posA != posC);
            Assert.IsFalse(posA.Equals(null));
            Assert.IsFalse(posA.Equals("string"));

            StringAssert.Contains("1.5", posA.ToString());
            StringAssert.Contains("-2.5", posA.ToString());
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-10)]
        public void CalculatePositions_ReturnsEmpty_WhenCountZeroOrNegative(int count)
        {
            FormationPosition[] positions = FormationSolver.CalculatePositions(count);

            Assert.IsNotNull(positions);
            Assert.AreEqual(0, positions.Length);
        }

        [Test]
        [TestCase(0f)]
        [TestCase(-0.5f)]
        public void CalculatePositions_Throws_WhenSpacingZeroOrNegative(float invalidSpacing)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FormationSolver.CalculatePositions(3, invalidSpacing, 5));
        }

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        public void CalculatePositions_Throws_WhenMaxPerRowZeroOrNegative(int invalidMaxPerRow)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FormationSolver.CalculatePositions(3, 0.5f, invalidMaxPerRow));
        }

        [Test]
        public void CalculatePositions_SingleUnit_CenteredBehindLeader()
        {
            FormationPosition[] positions = FormationSolver.CalculatePositions(1, spacing: 0.5f, maxPerRow: 5);

            Assert.AreEqual(1, positions.Length);
            Assert.AreEqual(0.0f, positions[0].X, Tolerance);
            Assert.AreEqual(-0.5f, positions[0].Z, Tolerance);
        }

        [Test]
        public void CalculatePositions_OddCountSingleRow_SymmetricAroundZero()
        {
            const float spacing = 0.5f;
            FormationPosition[] positions = FormationSolver.CalculatePositions(3, spacing, maxPerRow: 5);

            Assert.AreEqual(3, positions.Length);

            Assert.AreEqual(-spacing, positions[0].X, Tolerance);
            Assert.AreEqual(-spacing, positions[0].Z, Tolerance);

            Assert.AreEqual(0.0f, positions[1].X, Tolerance);
            Assert.AreEqual(-spacing, positions[1].Z, Tolerance);

            Assert.AreEqual(spacing, positions[2].X, Tolerance);
            Assert.AreEqual(-spacing, positions[2].Z, Tolerance);
        }

        [Test]
        public void CalculatePositions_EvenCountSingleRow_SymmetricAroundZero()
        {
            const float spacing = 0.5f;
            FormationPosition[] positions = FormationSolver.CalculatePositions(2, spacing, maxPerRow: 5);

            Assert.AreEqual(2, positions.Length);

            Assert.AreEqual(-0.5f * spacing, positions[0].X, Tolerance);
            Assert.AreEqual(-spacing, positions[0].Z, Tolerance);

            Assert.AreEqual(0.5f * spacing, positions[1].X, Tolerance);
            Assert.AreEqual(-spacing, positions[1].Z, Tolerance);
        }

        [Test]
        public void CalculatePositions_MultipleRows_DistributesAcrossRowsWithOrderedNegativeZ()
        {
            const float spacing = 0.5f;
            const int maxPerRow = 5;
            FormationPosition[] positions = FormationSolver.CalculatePositions(7, spacing, maxPerRow);

            Assert.AreEqual(7, positions.Length);

            // Linha 0 (5 soldados)
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(-spacing, positions[i].Z, Tolerance);
            }
            Assert.AreEqual(-2 * spacing, positions[0].X, Tolerance);
            Assert.AreEqual(-1 * spacing, positions[1].X, Tolerance);
            Assert.AreEqual(0.0f, positions[2].X, Tolerance);
            Assert.AreEqual(1 * spacing, positions[3].X, Tolerance);
            Assert.AreEqual(2 * spacing, positions[4].X, Tolerance);

            // Linha 1 (2 soldados restantes)
            for (int i = 5; i < 7; i++)
            {
                Assert.AreEqual(-2 * spacing, positions[i].Z, Tolerance);
            }
            Assert.AreEqual(-0.5f * spacing, positions[5].X, Tolerance);
            Assert.AreEqual(0.5f * spacing, positions[6].X, Tolerance);

            // Todos os Z devem ser estritamente negativos (atrás do líder)
            foreach (var pos in positions)
            {
                Assert.Less(pos.Z, 0.0f);
            }
        }

        [Test]
        public void CalculatePositions_FullRows_DistributesEvenly()
        {
            const float spacing = 0.4f;
            const int maxPerRow = 4;
            FormationPosition[] positions = FormationSolver.CalculatePositions(8, spacing, maxPerRow);

            Assert.AreEqual(8, positions.Length);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(-spacing, positions[i].Z, Tolerance);
            }

            for (int i = 4; i < 8; i++)
            {
                Assert.AreEqual(-2 * spacing, positions[i].Z, Tolerance);
            }
        }
    }
}
