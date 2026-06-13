using System;
using System.Threading.Tasks;
using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>Configurable fake provider — completes synchronously so the
    /// coordinator's awaits resolve inline (safe to GetResult in a sync test).</summary>
    internal sealed class FakeCloudProvider : ICloudSaveProvider
    {
        public bool Available = true;
        public bool AuthResult = true;
        public bool ThrowOnAuth;
        public string CloudJson;
        public bool SaveResult = true;
        public int SaveCalls;

        public bool IsAvailable => Available;

        public Task<bool> AuthenticateAsync()
        {
            if (ThrowOnAuth) throw new Exception("auth blew up");
            return Task.FromResult(AuthResult);
        }

        public Task<string> LoadAsync() => Task.FromResult(CloudJson);

        public Task<bool> SaveAsync(string json)
        {
            SaveCalls++;
            return Task.FromResult(SaveResult);
        }
    }

    /// <summary>
    /// Gate D: the auth/load/reconcile/push orchestration — the structural
    /// guarantee that cloud never blocks and always falls to local.
    /// </summary>
    public class CloudSyncCoordinatorTests
    {
        private static T Run<T>(Task<T> task) => task.GetAwaiter().GetResult();

        private static SaveData Local(int gen, double ase)
        {
            var s = new SaveData();
            s.lineage.generationCount = gen;
            s.SetAse(BigNumber.FromDouble(ase));
            return s;
        }

        [Test]
        public void NoProvider_ReturnsLocal_Untouched()
        {
            var coord = new CloudSyncCoordinator(null);
            var local = Local(2, 100);
            Assert.AreSame(local, Run(coord.AuthenticateAndReconcileAsync(local)));
            Assert.IsFalse(coord.IsAuthenticated);
        }

        [Test]
        public void UnavailableProvider_ShortCircuitsToLocal()
        {
            var coord = new CloudSyncCoordinator(new FakeCloudProvider { Available = false });
            var local = Local(2, 100);
            Assert.AreSame(local, Run(coord.AuthenticateAndReconcileAsync(local)));
        }

        [Test]
        public void AuthFails_KeepsLocal_NeverThrows()
        {
            var coord = new CloudSyncCoordinator(new FakeCloudProvider { AuthResult = false });
            var local = Local(2, 100);
            Assert.AreSame(local, Run(coord.AuthenticateAndReconcileAsync(local)));
            Assert.IsFalse(coord.IsAuthenticated);
        }

        [Test]
        public void AuthThrows_IsSwallowed_KeepsLocal()
        {
            var coord = new CloudSyncCoordinator(new FakeCloudProvider { ThrowOnAuth = true });
            var local = Local(2, 100);
            Assert.DoesNotThrow(() => Run(coord.AuthenticateAndReconcileAsync(local)));
            Assert.AreSame(local, Run(coord.AuthenticateAndReconcileAsync(local)));
        }

        [Test]
        public void AuthSucceeds_NewerCloud_IsAdopted()
        {
            var cloud = Local(5, 10);
            var provider = new FakeCloudProvider { CloudJson = SaveSerializer.ToJson(cloud) };
            var coord = new CloudSyncCoordinator(provider);
            var local = Local(2, 9_000_000);

            var chosen = Run(coord.AuthenticateAndReconcileAsync(local));

            Assert.IsTrue(coord.IsAuthenticated);
            Assert.AreNotSame(local, chosen);
            Assert.AreEqual(5, chosen.lineage.generationCount, "newer cloud generation adopted");
        }

        [Test]
        public void AuthSucceeds_OlderCloud_KeepsLocal()
        {
            var cloud = Local(1, 10);
            var provider = new FakeCloudProvider { CloudJson = SaveSerializer.ToJson(cloud) };
            var coord = new CloudSyncCoordinator(provider);
            var local = Local(3, 100);

            var chosen = Run(coord.AuthenticateAndReconcileAsync(local));

            Assert.AreSame(local, chosen, "local is ahead — keep it");
            Assert.IsTrue(coord.IsAuthenticated);
        }

        [Test]
        public void AuthSucceeds_NoCloudSave_KeepsLocal()
        {
            var provider = new FakeCloudProvider { CloudJson = null };
            var coord = new CloudSyncCoordinator(provider);
            var local = Local(3, 100);
            Assert.AreSame(local, Run(coord.AuthenticateAndReconcileAsync(local)));
        }

        [Test]
        public void Push_RequiresAuth()
        {
            var provider = new FakeCloudProvider { AuthResult = false };
            var coord = new CloudSyncCoordinator(provider);
            Run(coord.AuthenticateAndReconcileAsync(Local(1, 1)));

            Assert.IsFalse(Run(coord.PushAsync("{}")), "no push before auth");
            Assert.AreEqual(0, provider.SaveCalls);
        }

        [Test]
        public void Push_AfterAuth_CallsProvider()
        {
            var provider = new FakeCloudProvider { CloudJson = null };
            var coord = new CloudSyncCoordinator(provider);
            Run(coord.AuthenticateAndReconcileAsync(Local(1, 1)));

            Assert.IsTrue(Run(coord.PushAsync("{}")));
            Assert.AreEqual(1, provider.SaveCalls);
        }
    }
}
