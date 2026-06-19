using NUnit.Framework;
using OriAscendant.Core;
using OriAscendant.Save;
using UnityEngine;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// End-to-end proof for the locked CLAUDE.md rule "cloud auth/load/push
    /// failure → fall through to local, never block gameplay." Drives the
    /// failure modes through the real <see cref="CloudSaveManager"/> (not the
    /// coordinator in isolation), so a regression in either the manager OR
    /// the coordinator trips the gate.
    ///
    /// The FakeCloudProvider completes every async call synchronously, so the
    /// manager's async-void reconcile and fire-and-forget push both run inline
    /// — these assertions are deterministic, no waits or polling.
    /// </summary>
    public class CloudFailureEndToEndTests
    {
        private GameObject _host;
        private CloudSaveManager _cloud;

        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            _host = new GameObject("CloudFailureHost");
            _cloud = _host.AddComponent<CloudSaveManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
            ServiceLocator.Clear();
        }

        private static SaveData MakeLocal(int gen, double ase)
        {
            var s = new SaveData();
            s.lineage.generationCount = gen;
            s.SetAse(BigNumber.FromDouble(ase));
            return s;
        }

        [Test]
        public void AuthThrowsAfterGameplayStarts_LocalReturned_NoExceptionEscapes_StateUnchanged()
        {
            // Simulates the real-world hazard: gameplay is already running on
            // the local save when iCloud auth blows up in the background.
            _cloud.Initialize(new FakeCloudProvider { ThrowOnAuth = true });
            var local = MakeLocal(3, 1_000_000);

            int adoptions = 0;
            _cloud.OnCloudSaveAdopted += _ => adoptions++;

            Assert.DoesNotThrow(() => _cloud.BeginBackgroundReconcile(local));

            Assert.AreEqual(0, adoptions, "an auth blow-up must never trigger adoption");
            Assert.IsFalse(_cloud.Coordinator.IsAuthenticated, "auth failed → not authenticated");
            Assert.AreEqual(3, local.lineage.generationCount, "local generation untouched");
            Assert.AreEqual(BigNumber.FromDouble(1_000_000), local.GetAse(), "local Àṣẹ untouched");
        }

        [Test]
        public void AuthReturnsFalseAfterStart_NoAdoption_LocalIntact()
        {
            // The other auth failure shape — provider says "no" without throwing
            // (offline, no Apple ID, iCloud off). The reconcile call must still
            // fall through quietly so background gameplay is unaffected.
            _cloud.Initialize(new FakeCloudProvider { AuthResult = false });
            var local = MakeLocal(2, 500);

            int adoptions = 0;
            _cloud.OnCloudSaveAdopted += _ => adoptions++;

            Assert.DoesNotThrow(() => _cloud.BeginBackgroundReconcile(local));

            Assert.AreEqual(0, adoptions);
            Assert.IsFalse(_cloud.Coordinator.IsAuthenticated);
            Assert.AreEqual(2, local.lineage.generationCount);
        }

        [Test]
        public void PushThrowsDuringUpload_SwallowedSilently_GameplayContinues_StateIntact()
        {
            // The other locked rule: a push that throws (Game Center hiccup,
            // network drop mid-upload) must NEVER surface. The coordinator
            // catches it; the manager defers and trusts the coordinator's contract.
            var provider = new FakeCloudProvider { AuthResult = true, ThrowOnSave = true };
            _cloud.Initialize(provider);

            // Wire up a real SaveManager so PushLatest has something to serialize.
            var saveManager = _host.AddComponent<SaveManager>();
            SaveData live = saveManager.Load();
            live.lineage.generationCount = 4;
            live.SetAse(BigNumber.FromDouble(9_000_000));

            // Reconcile authenticates the coordinator — required before PushLatest
            // hits the provider at all (Push_RequiresAuth).
            Assert.DoesNotThrow(() => _cloud.BeginBackgroundReconcile(live));
            Assert.IsTrue(_cloud.Coordinator.IsAuthenticated);

            int adoptions = 0;
            _cloud.OnCloudSaveAdopted += _ => adoptions++;

            Assert.DoesNotThrow(() => _cloud.PushLatest(),
                "a push that throws must not propagate to the caller");

            Assert.AreEqual(1, _cloud.PushRequestCount, "the hook still fires synchronously");
            Assert.AreEqual(1, provider.SaveCalls, "the provider's SaveAsync was actually invoked");
            Assert.AreEqual(0, adoptions, "a failed push must never trigger adoption");
            Assert.AreEqual(4, live.lineage.generationCount, "live save state intact");
            Assert.AreEqual(BigNumber.FromDouble(9_000_000), live.GetAse(),
                "live Àṣẹ intact after a failed push");
        }

        [Test]
        public void LoadThrowsDuringReconcile_NoAdoption_LocalIntact()
        {
            // Auth succeeds; cloud blob download itself blows up (server 5xx,
            // truncated transfer). Coordinator's catch keeps the chosen save = local;
            // manager's reconcile path must not double-catch nor re-assert the rule.
            _cloud.Initialize(new FakeCloudProvider { AuthResult = true, ThrowOnLoad = true });
            var local = MakeLocal(2, 100);

            int adoptions = 0;
            _cloud.OnCloudSaveAdopted += _ => adoptions++;

            Assert.DoesNotThrow(() => _cloud.BeginBackgroundReconcile(local));

            Assert.AreEqual(0, adoptions);
            Assert.IsTrue(_cloud.Coordinator.IsAuthenticated, "auth still passed");
            Assert.AreEqual(2, local.lineage.generationCount);
            Assert.AreEqual(BigNumber.FromDouble(100), local.GetAse());
        }
    }
}
