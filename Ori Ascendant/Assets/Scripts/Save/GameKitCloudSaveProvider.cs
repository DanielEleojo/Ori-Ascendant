// GameKit-backed cloud save (Apple.GameKit GKSavedGame). This entire file
// compiles to NOTHING off-device (#if UNITY_IOS && !UNITY_EDITOR), so the Linux
// editor needs no Apple assembly reference. When the Halfbrick prebuilt
// Apple.Core + Apple.GameKit .tgz are committed (see BUILD_PLAN Phase D
// checklist), add "Apple.Core" and "Apple.GameKit" to OriAscendant.asmdef
// references and the file: entries to Packages/manifest.json.
//
// SCAFFOLD NOTE: written faithfully to the GKSavedGame API shape the research
// surfaced, but it has never been compiled against the real package on this
// Linux box. Verify the exact Apple.GameKit 3.0.2 signatures on the first iOS
// build, and keep an early TestFlight build to shake out auth/entitlements.
// APPLE_GAMEKIT define gate: this file only compiles once the Apple.Core +
// Apple.GameKit packages are wired in (Phase D). Until that define is added to
// the iOS player settings, the device build falls back to NullCloudSaveProvider
// (local save only) — see CloudSaveManager.CreateProvider.
#if UNITY_IOS && !UNITY_EDITOR && APPLE_GAMEKIT
using System;
using System.Text;
using System.Threading.Tasks;
using Apple.GameKit;
using UnityEngine;

namespace OriAscendant.Save
{
    public sealed class GameKitCloudSaveProvider : ICloudSaveProvider
    {
        private const string SaveName = "OriAscendantSave";

        public bool IsAvailable => true;

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var player = await GKLocalPlayer.Authenticate();
                return player != null && player.IsAuthenticated;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GameKit auth failed, falling to local: {e.Message}");
                return false; // never throws past here
            }
        }

        public async Task<string> LoadAsync()
        {
            try
            {
                var saves = await FetchSavedGamesWithRetry();
                if (saves == null) return null;

                GKSavedGame newest = null;
                foreach (var s in saves)
                {
                    if (newest == null || s.ModificationDate > newest.ModificationDate) newest = s;
                }
                if (newest == null) return null;

                var data = await newest.LoadData();
                return data == null ? null : Encoding.UTF8.GetString(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GameKit load failed: {e.Message}");
                return null;
            }
        }

        public async Task<bool> SaveAsync(string json)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await GKLocalPlayer.Local.SaveGameData(bytes, SaveName);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"GameKit save failed: {e.Message}");
                return false;
            }
        }

        // Fresh installs sometimes return an empty set on the first call — retry once.
        private static async Task<GKSavedGame[]> FetchSavedGamesWithRetry()
        {
            var saves = await GKLocalPlayer.Local.FetchSavedGames();
            if (saves == null || saves.Count == 0)
            {
                saves = await GKLocalPlayer.Local.FetchSavedGames();
            }
            if (saves == null) return null;
            var array = new GKSavedGame[saves.Count];
            for (int i = 0; i < saves.Count; i++) array[i] = saves[i];
            return array;
        }
    }
}
#endif
