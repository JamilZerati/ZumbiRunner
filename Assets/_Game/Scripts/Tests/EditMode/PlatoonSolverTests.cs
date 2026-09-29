using System;
using System.Linq;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class PlatoonSolverTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void PlatoonEmitter_PropriedadesEIgualdade()
        {
            var posA = new FormationPosition(1f, -1f);
            var posB = new FormationPosition(1f, -1f);
            var posC = new FormationPosition(0f, -1f);

            var emitterA = new PlatoonEmitter(0, 5, posA);
            var emitterB = new PlatoonEmitter(0, 5, posB);
            var emitterC = new PlatoonEmitter(1, 5, posA);
            var emitterD = new PlatoonEmitter(0, 4, posA);
            var emitterE = new PlatoonEmitter(0, 5, posC);

            Assert.AreEqual(0, emitterA.Index);
            Assert.AreEqual(5, emitterA.SoldierCount);
            Assert.AreEqual(posA, emitterA.Position);

            Assert.IsTrue(emitterA.Equals(emitterB));
            Assert.IsTrue(emitterA.Equals((object)emitterB));
            Assert.AreEqual(emitterA.GetHashCode(), emitterB.GetHashCode());

            Assert.IsFalse(emitterA.Equals(emitterC));
            Assert.IsFalse(emitterA.Equals(emitterD));
            Assert.IsFalse(emitterA.Equals(emitterE));
            Assert.IsFalse(emitterA.Equals(null));
        }

        [Test]
        public void CalculatePlatoons_ComTropaDez_RetornaDoisEmissoresDeCincoSoldados()
        {
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(10);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(2, platoons.Length);

            Assert.AreEqual(0, platoons[0].Index);
            Assert.AreEqual(5, platoons[0].SoldierCount);

            Assert.AreEqual(1, platoons[1].Index);
            Assert.AreEqual(5, platoons[1].SoldierCount);
        }

        [Test]
        public void CalculatePlatoons_ComTropaUm_RetornaUmEmissorDeUmSoldado()
        {
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(1);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(1, platoons.Length);
            Assert.AreEqual(0, platoons[0].Index);
            Assert.AreEqual(1, platoons[0].SoldierCount);
        }

        [Test]
        public void CalculatePlatoons_ComTropaSete_DistribuiRestoNoPrimeiroEmissor()
        {
            // 7 soldados -> ceil(7/5) = 2 emissores. 7 / 2 = 3 base, resto 1.
            // Emissor 0: 4 soldados. Emissor 1: 3 soldados.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(7);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(2, platoons.Length);
            Assert.AreEqual(4, platoons[0].SoldierCount);
            Assert.AreEqual(3, platoons[1].SoldierCount);
            Assert.AreEqual(7, platoons[0].SoldierCount + platoons[1].SoldierCount);
        }

        [Test]
        public void CalculatePlatoons_ComTropaDoze_DistribuiRestoNosDoisPrimeirosEmissores()
        {
            // 12 soldados -> ceil(12/5) = 3 emissores. 12 / 3 = 4 base, resto 0.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(12);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(3, platoons.Length);
            Assert.AreEqual(4, platoons[0].SoldierCount);
            Assert.AreEqual(4, platoons[1].SoldierCount);
            Assert.AreEqual(4, platoons[2].SoldierCount);
            Assert.AreEqual(12, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_ComTropaCinco_RetornaUmEmissorDeCincoSoldados()
        {
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(5);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(1, platoons.Length);
            Assert.AreEqual(5, platoons[0].SoldierCount);
        }

        [Test]
        public void CalculatePlatoons_ComTropaSeis_RetornaDoisEmissoresComTresSoldados()
        {
            // 6 soldados -> ceil(6/5) = 2 emissores. 6 / 2 = 3 base, resto 0.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(6);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(2, platoons.Length);
            Assert.AreEqual(3, platoons[0].SoldierCount);
            Assert.AreEqual(3, platoons[1].SoldierCount);
            Assert.AreEqual(6, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_ComTropaDuzentos_AtingeTetoDeQuarentaEmissoresDeCinco()
        {
            // 200 soldados -> min(ceil(200/5), 40) = 40 emissores.
            // 200 / 40 = 5 base, resto 0.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(200);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(40, platoons.Length);
            for (int i = 0; i < 40; i++)
            {
                Assert.AreEqual(i, platoons[i].Index);
                Assert.AreEqual(5, platoons[i].SoldierCount);
            }
            Assert.AreEqual(200, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_ComTropaTrezentos_MantemTetoDeQuarentaComSoldadosExtras()
        {
            // 300 soldados -> teto 40 emissores.
            // 300 / 40 = 7 base, resto 20.
            // Primeiros 20 emissores: 8 soldados. Ultimos 20 emissores: 7 soldados.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(300);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(40, platoons.Length);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(8, platoons[i].SoldierCount);
            }
            for (int i = 20; i < 40; i++)
            {
                Assert.AreEqual(7, platoons[i].SoldierCount);
            }
            Assert.AreEqual(300, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_EmissoresAcompanhamPosicoesDoFormationSolver()
        {
            const float spacing = 0.6f;
            const int maxPerRow = 5;
            FormationPosition[] expectedPositions = FormationSolver.CalculatePositions(2, spacing, maxPerRow);

            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(10, spacing, maxPerRow);

            Assert.AreEqual(2, platoons.Length);
            Assert.AreEqual(expectedPositions[0].X, platoons[0].Position.X, Tolerance);
            Assert.AreEqual(expectedPositions[0].Z, platoons[0].Position.Z, Tolerance);
            Assert.AreEqual(expectedPositions[1].X, platoons[1].Position.X, Tolerance);
            Assert.AreEqual(expectedPositions[1].Z, platoons[1].Position.Z, Tolerance);
        }

        // --- Cenários Adversários (Edge-Case Adversary) ---

        [Test]
        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(-100)]
        public void CalculatePlatoons_ComTropaZeroOuNegativa_RetornaArrayVazioSemExcecao(int squadCount)
        {
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(squadCount);

            Assert.IsNotNull(platoons);
            Assert.AreEqual(0, platoons.Length);
        }

        [Test]
        public void CalculatePlatoons_ComTropa199_PreservaSomaExataDeSoldados()
        {
            // 199 soldados -> ceil(199/5) = 40 emissores.
            // 199 / 40 = 4 base, resto 39.
            // Primeiros 39 emissores: 5 soldados. Ultimo emissor: 4 soldados.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(199);

            Assert.AreEqual(40, platoons.Length);
            for (int i = 0; i < 39; i++)
            {
                Assert.AreEqual(5, platoons[i].SoldierCount);
            }
            Assert.AreEqual(4, platoons[39].SoldierCount);
            Assert.AreEqual(199, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_ComTropa205_PreservaSomaExataDeSoldados()
        {
            // 205 soldados -> teto 40 emissores.
            // 205 / 40 = 5 base, resto 5.
            // Primeiros 5 emissores: 6 soldados. Ultimos 35 emissores: 5 soldados.
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(205);

            Assert.AreEqual(40, platoons.Length);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(6, platoons[i].SoldierCount);
            }
            for (int i = 5; i < 40; i++)
            {
                Assert.AreEqual(5, platoons[i].SoldierCount);
            }
            Assert.AreEqual(205, platoons.Sum(p => p.SoldierCount));
        }

        [Test]
        public void CalculatePlatoons_ComSpacingEMaxPerRowCustomizados_PropagaParaPosicoes()
        {
            const float customSpacing = 1.2f;
            const int customMaxPerRow = 3;

            FormationPosition[] expectedPositions = FormationSolver.CalculatePositions(4, customSpacing, customMaxPerRow);
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(20, customSpacing, customMaxPerRow);

            Assert.AreEqual(4, platoons.Length);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(expectedPositions[i].X, platoons[i].Position.X, Tolerance);
                Assert.AreEqual(expectedPositions[i].Z, platoons[i].Position.Z, Tolerance);
            }
        }

        [Test]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(199)]
        [TestCase(200)]
        [TestCase(205)]
        [TestCase(300)]
        public void CalculatePlatoons_InvarianteDaSoma_TotalDeSoldadosNosEmissoresIgualAoOriginal(int squadCount)
        {
            PlatoonEmitter[] platoons = PlatoonSolver.CalculatePlatoons(squadCount);

            int totalSoldiers = platoons.Sum(p => p.SoldierCount);
            Assert.AreEqual(squadCount, totalSoldiers);
        }
    }
}
