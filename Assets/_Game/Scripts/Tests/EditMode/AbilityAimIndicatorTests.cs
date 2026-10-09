using NUnit.Framework;
using UnityEngine;
using Game.Presentation;

namespace Game.Tests.EditMode
{
    public class AbilityAimIndicatorTests
    {
        private GameObject _indicatorGo;
        private AbilityAimIndicator _indicator;

        [SetUp]
        public void Setup()
        {
            _indicatorGo = new GameObject("AimIndicator");
            _indicator = _indicatorGo.AddComponent<AbilityAimIndicator>();
            _indicator.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (_indicatorGo != null)
            {
                Object.DestroyImmediate(_indicatorGo);
            }
        }

        [Test]
        public void Inicializa_Escondido()
        {
            Assert.IsFalse(_indicator.IsVisible);
        }

        [Test]
        public void Show_TornaIndicadorVisivel()
        {
            _indicator.Show(radius: 4f);
            Assert.IsTrue(_indicator.IsVisible);
            
            // Should adjust the scale to match the radius. The scale is diameter, so 2 * radius, but let's check what the spec says or what makes sense.
            // A primitive cylinder of size 1x1x1 has radius 0.5. So diameter is 1. If radius is 4, diameter is 8.
            // Let's assume the indicator object is 1x1x1 by default, so its scale should be diameter (radius * 2).
            Assert.AreEqual(new Vector3(8f, 0.1f, 8f), _indicatorGo.transform.localScale);
        }

        [Test]
        public void Hide_OcultaOIndicador()
        {
            _indicator.Show(4f);
            _indicator.Hide();
            Assert.IsFalse(_indicator.IsVisible);
        }

        [Test]
        public void UpdateAim_MudaAPosicaoEAVerificaSeEstaCancelando()
        {
            _indicator.Show(4f);
            
            Vector3 worldPos = new Vector3(2f, 0f, 10f);
            _indicator.UpdateAim(worldPos, isCanceling: true);

            Assert.AreEqual(new Vector3(2f, 0.05f, 10f), _indicatorGo.transform.position);
            Assert.IsTrue(_indicator.IsCanceling);
        }
    }
}
