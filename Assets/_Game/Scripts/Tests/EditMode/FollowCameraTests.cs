using Game.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class FollowCameraTests
    {
        private GameObject targetGo;
        private GameObject cameraGo;
        private FollowCamera followCam;

        [SetUp]
        public void SetUp()
        {
            targetGo = new GameObject("Target");
            cameraGo = new GameObject("Camera");
            followCam = cameraGo.AddComponent<FollowCamera>();
            followCam.Target = targetGo.transform;
            followCam.Offset = new Vector3(0f, 7.0f, -9.0f);
        }

        [TearDown]
        public void TearDown()
        {
            if (targetGo != null)
            {
                Object.DestroyImmediate(targetGo);
            }

            if (cameraGo != null)
            {
                Object.DestroyImmediate(cameraGo);
            }
        }

        [Test]
        public void Snap_PositionsCameraExactlyAtTargetPlusOffset()
        {
            targetGo.transform.position = new Vector3(2.0f, 1.0f, 50.0f);

            followCam.Snap();

            Vector3 expected = new Vector3(2.0f, 8.0f, 41.0f);
            Assert.AreEqual(expected.x, followCam.transform.position.x, 1e-5f);
            Assert.AreEqual(expected.y, followCam.transform.position.y, 1e-5f);
            Assert.AreEqual(expected.z, followCam.transform.position.z, 1e-5f);
        }

        [Test]
        public void UpdateCameraPosition_LocksYAndZ_AndInterpolatesX()
        {
            targetGo.transform.position = new Vector3(0f, 0f, 0f);
            followCam.Snap();

            // Target moves to lane X = 2 and advances Z to 10
            targetGo.transform.position = new Vector3(2.0f, 0f, 10.0f);

            // Forward Z and height Y are locked to target + offset immediately
            followCam.UpdateCameraPosition(0.05f);

            Assert.AreEqual(7.0f, followCam.transform.position.y, 1e-5f);
            Assert.AreEqual(1.0f, followCam.transform.position.z, 1e-5f); // 10 + (-9) = 1
            // X is interpolated between 0 and 2
            Assert.Greater(followCam.transform.position.x, 0.0f);
            Assert.Less(followCam.transform.position.x, 2.0f);
        }
    }
}
