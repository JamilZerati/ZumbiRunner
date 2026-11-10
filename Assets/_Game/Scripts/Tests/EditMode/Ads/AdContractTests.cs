using System;
using Game.Core.Ads;
using Game.Infrastructure.Ads;
using NUnit.Framework;

namespace Game.Tests.EditMode.Ads
{
    public abstract class AdServiceContractTestsBase
    {
        protected abstract IAdService CreateAdService();

        [Test]
        public void Contract_IsAdAvailable_ReturnsBool_ForAnyPlacement()
        {
            var service = CreateAdService();
            foreach (AdPlacement placement in Enum.GetValues(typeof(AdPlacement)))
            {
                var isAvailable = service.IsAdAvailable(placement);
                Assert.IsInstanceOf<bool>(isAvailable);
            }
        }

        [Test]
        public void Contract_ShowAd_InvokesCallback_WithValidResult()
        {
            var service = CreateAdService();
            var callbackInvoked = false;
            var receivedResult = AdShowResult.NotAvailable;

            service.ShowAd(AdPlacement.DoubleReward, result =>
            {
                callbackInvoked = true;
                receivedResult = result;
            });

            Assert.IsTrue(callbackInvoked);
            Assert.IsTrue(Enum.IsDefined(typeof(AdShowResult), receivedResult));
        }

        [Test]
        public void Contract_ShowAd_WithNullCallback_DoesNotThrowException()
        {
            var service = CreateAdService();
            Assert.DoesNotThrow(() => service.ShowAd(AdPlacement.DoubleReward, null));
        }

        [Test]
        public void Contract_ShowAd_AllPlacements_CompleteWithoutUnhandledExceptions()
        {
            var service = CreateAdService();
            foreach (AdPlacement placement in Enum.GetValues(typeof(AdPlacement)))
            {
                var invoked = false;
                Assert.DoesNotThrow(() => service.ShowAd(placement, _ => invoked = true));
                Assert.IsTrue(invoked);
            }
        }
    }

    [TestFixture]
    public class MockAdServiceContractTests : AdServiceContractTestsBase
    {
        protected override IAdService CreateAdService()
        {
            return new MockAdService();
        }
    }

    [TestFixture]
    [Ignore("Red comprovado no Marco 0: o adapter de producao LevelPlayAdService e implementado no Marco 4 (NEX-1500).")]
    public class RealAdServiceContractTests : AdServiceContractTestsBase
    {
        protected override IAdService CreateAdService()
        {
            return new LevelPlayAdService();
        }
    }
}
