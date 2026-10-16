using System;
using System.Collections.Generic;
using Game.Meta.Tracks;
using NUnit.Framework;

namespace Game.Tests.EditMode.Meta
{
    public class EntitlementsTests
    {
        private Entitlements _entitlements;

        [SetUp]
        public void SetUp()
        {
            _entitlements = new Entitlements();
        }

        [Test]
        public void HasEntitlement_ReturnsFalse_Initially()
        {
            Assert.IsFalse(_entitlements.HasEntitlement("battlepass_s1"));
            Assert.IsFalse(_entitlements.HasEntitlement(null));
            Assert.IsFalse(_entitlements.HasEntitlement(""));
        }

        [Test]
        public void GrantEntitlement_GrantsEntitlement_AndIsQueryable()
        {
            _entitlements.GrantEntitlement("battlepass_s1");

            Assert.IsTrue(_entitlements.HasEntitlement("battlepass_s1"));
            Assert.IsTrue(_entitlements.HasEntitlement("BATTLEPASS_S1"));
            CollectionAssert.Contains(_entitlements.GetAllEntitlements(), "battlepass_s1");
        }

        [Test]
        public void GrantEntitlement_ThrowsArgumentException_WhenIdIsNullOrWhitespace()
        {
            Assert.Throws<ArgumentException>(() => _entitlements.GrantEntitlement(null));
            Assert.Throws<ArgumentException>(() => _entitlements.GrantEntitlement("   "));
        }

        [Test]
        public void RevokeEntitlement_RemovesEntitlement()
        {
            _entitlements.GrantEntitlement("vip_pass");
            Assert.IsTrue(_entitlements.HasEntitlement("vip_pass"));

            _entitlements.RevokeEntitlement("vip_pass");
            Assert.IsFalse(_entitlements.HasEntitlement("vip_pass"));
        }
    }
}
