using NUnit.Framework;
using OriAscendant.Core;

namespace OriAscendant.Tests.EditMode
{
    public class ServiceLocatorTests
    {
        private sealed class FakeService { }

        [TearDown]
        public void TearDown() => ServiceLocator.Clear();

        [Test]
        public void RegisterAndGet_ReturnsSameInstance()
        {
            var service = new FakeService();
            ServiceLocator.Register(service);

            Assert.AreSame(service, ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void Get_Unregistered_Throws()
        {
            Assert.Throws<System.InvalidOperationException>(() => ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void TryGet_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(ServiceLocator.TryGet<FakeService>(out var service));
            Assert.IsNull(service);
        }

        [Test]
        public void Register_Replaces_PriorInstance()
        {
            var first = new FakeService();
            var second = new FakeService();
            ServiceLocator.Register(first);
            ServiceLocator.Register(second);

            Assert.AreSame(second, ServiceLocator.Get<FakeService>());
        }

        [Test]
        public void Unregister_OnlyRemoves_OwnInstance()
        {
            var current = new FakeService();
            var stale = new FakeService();
            ServiceLocator.Register(current);

            ServiceLocator.Unregister(stale); // a destroyed duplicate must not evict the live one
            Assert.AreSame(current, ServiceLocator.Get<FakeService>());

            ServiceLocator.Unregister(current);
            Assert.IsFalse(ServiceLocator.TryGet<FakeService>(out _));
        }
    }
}
