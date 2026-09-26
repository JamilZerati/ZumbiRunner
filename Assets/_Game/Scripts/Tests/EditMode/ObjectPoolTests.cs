using Game.Infrastructure;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class ObjectPoolTests
    {
        private class TestItem
        {
            public bool IsActive;
        }

        [Test]
        public void ObjectPool_RentsAndReturnsItems()
        {
            var pool = new ObjectPool<TestItem>(
                factory: () => new TestItem(),
                onRent: item => item.IsActive = true,
                onReturn: item => item.IsActive = false,
                initialCapacity: 2
            );

            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(2, pool.CountInactive);

            var first = pool.Rent();
            Assert.IsTrue(first.IsActive);
            Assert.AreEqual(1, pool.CountActive);
            Assert.AreEqual(1, pool.CountInactive);

            pool.Return(first);
            Assert.IsFalse(first.IsActive);
            Assert.AreEqual(0, pool.CountActive);
            Assert.AreEqual(2, pool.CountInactive);

            var reused = pool.Rent();
            Assert.AreSame(first, reused);
        }
    }
}
