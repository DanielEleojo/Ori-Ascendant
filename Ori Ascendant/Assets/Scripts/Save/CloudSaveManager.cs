using System;
using OriAscendant.Core;
using UnityEngine;

namespace OriAscendant.Save
{
    /// <summary>
    /// Game Center / iCloud save coordination (TECH_DESIGN §4). OFF-LIMITS RULES
    /// (CLAUDE.md): async only, and ALWAYS a failure fallback to local — auth is
    /// never awaited before gameplay and never blocks.
    ///
    /// Compliance by construction: gameplay always starts from the local save
    /// (GameManager). This manager runs auth+reconcile as fire-and-forget in the
    /// background and only raises <see cref="OnCloudSaveAdopted"/> in the rare
    /// case the cloud strictly wins (device swap / reinstall). All pushes are
    /// opportunistic and silent. The "any cloud failure → local, never throw"
    /// invariant is owned by <see cref="CloudSyncCoordinator"/> (the single
    /// seam, issue #15) — this manager calls into it and trusts the contract,
    /// no re-asserted try/catch in the reconcile or push paths. In the editor
    /// / on Linux the provider is <see cref="NullCloudSaveProvider"/>, so every
    /// path is inert.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        /// <summary>Raised (main-thread, from an async continuation) only when a
        /// strictly-newer cloud save should replace the running local one.</summary>
        public event Action<SaveData> OnCloudSaveAdopted;

        /// <summary>Incremented synchronously each time a push is requested —
        /// lets tests assert the hooks fire without racing the async upload.</summary>
        public int PushRequestCount { get; private set; }

        public CloudSyncCoordinator Coordinator { get; private set; }

        public string StatusLine =>
            Coordinator != null && Coordinator.IsAuthenticated
                ? "Game Center: connected"
                : "Local save only";

        private void Awake()
        {
            Initialize(CreateProvider());
            ServiceLocator.Register(this);
        }

        private void OnDestroy() => ServiceLocator.Unregister(this);

        /// <summary>Wires a provider + coordinator. Awake passes the platform
        /// provider; tests pass a fake.</summary>
        public void Initialize(ICloudSaveProvider provider)
        {
            Coordinator = new CloudSyncCoordinator(provider);
        }

        private static ICloudSaveProvider CreateProvider()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new GameKitCloudSaveProvider();
#else
            return new NullCloudSaveProvider();
#endif
        }

        /// <summary>Fire-and-forget launch reconcile. Never awaited by the caller;
        /// gameplay is already running on the local save when this starts. The
        /// coordinator's <see cref="CloudSyncCoordinator.AuthenticateAndReconcileAsync"/>
        /// owns the never-throws guarantee — this method defers to it.</summary>
        public async void BeginBackgroundReconcile(SaveData local)
        {
            if (Coordinator == null) return;
            SaveData chosen = await Coordinator.AuthenticateAndReconcileAsync(local);
            if (!ReferenceEquals(chosen, local) && chosen != null)
            {
                OnCloudSaveAdopted?.Invoke(chosen);
            }
        }

        /// <summary>Opportunistic push of the current in-memory save (serialized
        /// fresh, so it is correct regardless of the local file write order).
        /// Hooked from Tribulation completion (a locked business rule) and app
        /// suspend. The coordinator's <see cref="CloudSyncCoordinator.PushAsync"/>
        /// is silent on failure (the single owner of that rule) — no double-catch here.</summary>
        public void PushLatest()
        {
            PushRequestCount++; // synchronous: the "hook fired" signal for tests
            if (Coordinator == null) return;
            if (!ServiceLocator.TryGet(out SaveManager saveManager) || saveManager.Current == null) return;

            string json = SaveSerializer.ToJson(saveManager.Current);
            _ = Coordinator.PushAsync(json);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) PushLatest(); // best-effort, after the local write
        }
    }
}
