using Unity.Services.LevelPlay; // namespace verified against resolved package 9.4.1
using UnityEngine;

namespace OriAscendant.Ads
{
    /// <summary>
    /// Bottom banner ad via Unity LevelPlay — the supported successor to the
    /// Unity Ads "Advertisement Legacy" package (direct integration of which is
    /// unsupported as of 2026-01-31). Self-activates after scene load and persists
    /// across scenes, so it ships in a Cloud Build with zero scene wiring — same
    /// idiom as MainScreenSkin. Owns no game state; doesn't touch ServiceLocator.
    ///
    /// CONTENT SAFETY (no NSFW): LevelPlay exposes no code-level content switch —
    /// ad content is governed in the LevelPlay dashboard. Before release:
    ///   1. Dashboard → SDK Networks: enable only Unity Ads (brand-safe) until
    ///      you've vetted any others. Mediation = more networks = more surface.
    ///   2. App settings → ad content / age rating: set the lowest maturity
    ///      (family / general audiences).
    /// For a hard code-level "G-rated only" guarantee instead, AdMob's
    /// MaxAdContentRating.G is the alternative (heavier, non-Unity-registry setup).
    ///
    /// ATT is intentionally NOT prompted — ads serve (non-personalized) without it,
    /// which is simpler and privacy-safe. Add an ATT prompt later only if you want
    /// personalized-ad revenue.
    ///
    /// SETUP: fill AppKey + BannerAdUnitId from the LevelPlay dashboard
    /// (App settings). Until AppKey is set, this stays fully inert — no init, no
    /// banner — so the test gate and PlayMode tests are unaffected.
    /// </summary>
    public sealed class BannerAdController : MonoBehaviour
    {
        // Public app identifiers (not secrets). Fill from the LevelPlay dashboard.
        // static readonly (not const) so the "inert until configured" guard below
        // isn't constant-folded into an unreachable-code warning.
        private static readonly string AppKey = "YOUR_LEVELPLAY_APP_KEY";
        private static readonly string BannerAdUnitId = "YOUR_BANNER_AD_UNIT_ID";

        private LevelPlayBannerAd _banner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (AppKey == "YOUR_LEVELPLAY_APP_KEY") return; // inert until configured

            var go = new GameObject(nameof(BannerAdController));
            DontDestroyOnLoad(go);
            go.AddComponent<BannerAdController>();
        }

        private void Start()
        {
            LevelPlay.OnInitSuccess += OnInitSuccess;
            LevelPlay.OnInitFailed += OnInitFailed;
            LevelPlay.Init(AppKey);
        }

        // Ad objects must be created only after init succeeds (LevelPlay requirement).
        private void OnInitSuccess(LevelPlayConfiguration config)
        {
            var cfg = new LevelPlayBannerAd.Config.Builder()
                .SetSize(LevelPlayAdSize.BANNER)
                .SetPosition(LevelPlayBannerPosition.BottomCenter)
                .SetDisplayOnLoad(true)
                .Build();

            _banner = new LevelPlayBannerAd(BannerAdUnitId, cfg);
            _banner.OnAdLoadFailed += error => Debug.LogWarning($"[Ads] banner load failed: {error}");
            _banner.LoadAd();
        }

        private void OnInitFailed(LevelPlayInitError error)
            => Debug.LogWarning($"[Ads] LevelPlay init failed: {error}");
    }
}
