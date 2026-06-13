using System.Threading.Tasks;

namespace OriAscendant.Save
{
    /// <summary>
    /// Drives the cloud auth / load / reconcile / push flow against an injected
    /// <see cref="ICloudSaveProvider"/> (TECH_DESIGN §4). Plain C# so the whole
    /// orchestration is headless-testable with a fake provider. Every path is
    /// non-throwing and falls through to local — this class is WHY "cloud never
    /// blocks gameplay" holds structurally.
    /// </summary>
    public sealed class CloudSyncCoordinator
    {
        private readonly ICloudSaveProvider _provider;

        public bool IsAuthenticated { get; private set; }

        public CloudSyncCoordinator(ICloudSaveProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Authenticates, downloads the cloud save, and returns whichever save
        /// the game should adopt — LOCAL unless the cloud strictly wins
        /// (SaveConflictResolver). Returns the passed-in local on any failure or
        /// when no provider is available. Never throws.
        /// </summary>
        public async Task<SaveData> AuthenticateAndReconcileAsync(SaveData local)
        {
            IsAuthenticated = false;
            if (_provider == null || !_provider.IsAvailable) return local;

            bool authed;
            try { authed = await _provider.AuthenticateAsync(); }
            catch { authed = false; }
            if (!authed) return local; // fallback: keep local, never block

            IsAuthenticated = true;

            string cloudJson;
            try { cloudJson = await _provider.LoadAsync(); }
            catch { cloudJson = null; }

            SaveData cloud = SaveSerializer.FromJson(cloudJson);
            return SaveConflictResolver.Pick(local, cloud);
        }

        /// <summary>Best-effort upload. No-ops (returns false) unless authenticated
        /// with an available provider. Never throws.</summary>
        public async Task<bool> PushAsync(string json)
        {
            if (_provider == null || !_provider.IsAvailable || !IsAuthenticated) return false;
            try { return await _provider.SaveAsync(json); }
            catch { return false; }
        }
    }
}
