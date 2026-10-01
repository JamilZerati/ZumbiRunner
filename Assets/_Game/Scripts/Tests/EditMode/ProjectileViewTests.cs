using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class ProjectileViewTests
    {
        private GameObject testObject;
        private ProjectileView view;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestProjectileView");
            view = testObject.AddComponent<ProjectileView>();
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
            var testColor = new Color(1f, 0.5f, 0f, 1f);
            view.SetupVisuals(testColor);

            Assert.IsNotNull(view.MeshFilter);
            Assert.IsNotNull(view.MeshRenderer);
            Assert.IsNotNull(view.MeshFilter.sharedMesh);
            Assert.IsNotNull(view.MeshRenderer.sharedMaterial);
            Assert.AreEqual(testColor, view.MeshRenderer.sharedMaterial.color);
        }
    }
}
