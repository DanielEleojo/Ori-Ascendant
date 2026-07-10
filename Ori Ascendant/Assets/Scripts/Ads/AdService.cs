using System;
using Unity.Services.LevelPlay; // namespace verified against resolved package 9.4.1
using UnityEngine;

namespace OriAscendant.Ads
{
    /// <summary>
    /// Owns the single LevelPlay SDK init shared by every ad controller
    /// (banner, rewarded). Self-activates after scene load and persists across
    /// scenes — same idiom as BannerAdController. Owns no game state; doesn't
    /// touch ServiceLocator.
    ///
    /// Consumers subscribe to <see cref="OnInitialized"/> and create their ad
    /// objects there (LevelPlay requires init to succeed first). Late
    /// subscribers are invoked immediately, so bootstrap order among the ad
    /// controllers doesn't matter.
    ///
    /// SETUP: fill AppKey from the LevelPlay dashboard (App settings). Until
    /// AppKey is set, all ad code stays fully inert — no init, no GameObjects —
    /// so the test gate and PlayMode tests are unaffected.
    /// </summary>
    public sealed class AdService : MonoBehaviour
    {
        // Public app identifier (not a secret). Fill from the LevelPlay dashboard.
        // static readonly (not const) so the "inert until configured" guard below
        // isn't constant-folded into an unreachable-code warning.
        private static readonly string AppKey = "YOUR_LEVELPLAY_APP_KEY";

        /// <summary>Ad controllers gate their bootstraps on this — inert until the dashboard AppKey is filled in.</summary>
        internal static bool IsConfigured => AppKey != "YOUR_LEVELPLAY_APP_KEY";

        private static bool _initSucceeded;
        private static event Action Initialized;

        /// <summary>
        /// Fires once LevelPlay init succeeds. Subscribing after init has
        /// already succeeded invokes the handler immediately.
        /// </summary>
        public static event Action OnInitialized
        {
            add
            {
                Initialized += value;
                if (_initSucceeded) value?.Invoke();
            }
            remove => Initialized -= value;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!IsConfigured) return; // inert until configured

            var go = new GameObject(nameof(AdService));
            DontDestroyOnLoad(go);
            go.AddComponent<AdService>();
        }

        private void Start()
        {
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(AppKey);
        }

        private void OnInitSuccess(LevelPlayConfiguration config)
        {
            _initSucceeded = true;
            Initialized?.Invoke();
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"[Ads] LevelPlay init failed: {error}");
    }
}
