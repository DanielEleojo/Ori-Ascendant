using System.Threading.Tasks;

namespace OriAscendant.Save
{
    /// <summary>
    /// Platform-agnostic cloud save seam (TECH_DESIGN §4). Every method is async
    /// and NON-THROWING: failures return false/null, never exceptions, so the
    /// caller can always fall through to local save (the locked CloudSaveManager
    /// rule). The GameKit-backed implementation lives in
    /// GameKitCloudSaveProvider (compiled only on device); editor/standalone use
    /// <see cref="NullCloudSaveProvider"/>.
    /// </summary>
    public interface ICloudSaveProvider
    {
        /// <summary>False on platforms without a real provider (e.g. the Linux
        /// editor) — the coordinator short-circuits to local immediately.</summary>
        bool IsAvailable { get; }

        /// <summary>Authenticates the local player. Returns false on any failure
        /// (no Apple ID, iCloud off, offline) — never throws, never blocks.</summary>
        Task<bool> AuthenticateAsync();

        /// <summary>Downloads the cloud save JSON, or null if none / failed.</summary>
        Task<string> LoadAsync();

        /// <summary>Uploads the save JSON. Returns false on failure.</summary>
        Task<bool> SaveAsync(string json);
    }

    /// <summary>The fallback provider: no cloud, ever. Used in the editor and on
    /// any platform without GameKit, and whenever the real provider is absent.
    /// Keeps Linux Play-mode fully functional and makes "cloud failure falls to
    /// local" the structural default rather than an exception path.</summary>
    public sealed class NullCloudSaveProvider : ICloudSaveProvider
    {
        public bool IsAvailable => false;
        public Task<bool> AuthenticateAsync() => Task.FromResult(false);
        public Task<string> LoadAsync() => Task.FromResult<string>(null);
        public Task<bool> SaveAsync(string json) => Task.FromResult(false);
    }
}
