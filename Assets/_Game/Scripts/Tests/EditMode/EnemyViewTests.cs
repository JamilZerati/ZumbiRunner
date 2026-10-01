using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EnemyViewTests
    {
        private GameObject testObject;
        private EnemyView view;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestEnemyView");
            view = testObject.AddComponent<EnemyView>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void SetupVisuals_ConfiguresMeshRendererAndFilter_AndAppliesColor()
        {
            var testColor = Color.red;
            view.SetupVisuals(testColor);

            Assert.IsNotNull(view.MeshFilter);
            Assert.IsNotNull(view.MeshRenderer);
            Assert.IsNotNull(view.MeshFilter.sharedMesh);
            Assert.IsNotNull(view.MeshRenderer.sharedMaterial);
            Assert.AreEqual(testColor.r, view.MeshRenderer.sharedMaterial.color.r, 0.01f);
            Assert.AreEqual(testColor.g, view.MeshRenderer.sharedMaterial.color.g, 0.01f);
            Assert.AreEqual(testColor.b, view.MeshRenderer.sharedMaterial.color.b, 0.01f);
        }
    }
}
