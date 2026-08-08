using OriAscendant.Core;
using Unity.Services.LevelPlay; // namespace verified against resolved package 9.4.1
using UnityEngine;

namespace OriAscendant.Ads
{
    /// <summary>
    /// Rewarded ad via Unity LevelPlay. Self-activates after scene load and
    /// persists across scenes — same self-boot idiom used elsewhere in this
    /// codebase (see ProceduralAmbience). Registers in ServiceLocator so UI can
    /// TryGet it; while ads are inert (AppKey unset on
    /// AdService) it is simply absent, so callers treat "no service" as
    /// "no rewarded ads".
    ///
    /// REWARD INTEGRITY: <see cref="OnRewardGranted"/> fires ONLY from
    /// LevelPlay's OnAdRewarded callback — never on show or close — so a
    /// skipped, failed, or abandoned ad grants nothing.
    ///
    /// SETUP: fill RewardedAdUnitId here and AppKey on AdService (which owns
    /// the single LevelPlay init).
    /// </summary>
    public sealed class RewardedAdController : MonoBehaviour
    {
        // Public ad identifier (not a secret). Fill from the LevelPlay dashboard.
        private static readonly string RewardedAdUnitId = "5nety1y03c5jc1cn";

        private LevelPlayRewardedAd _ad;
        private bool _loaded;

        /// <summary>True while a rewarded ad is loaded and showable.</summary>
        public bool IsReady => _loaded;

        /// <summary>Fired once per completed reward — only from LevelPlay's OnAdRewarded.</summary>
        public event System.Action OnRewardGranted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!AdService.IsConfigured) return; // inert until configured

            var go = new GameObject(nameof(RewardedAdController));
            DontDestroyOnLoad(go);
            go.AddComponent<RewardedAdController>();
        }

        private void Awake() => ServiceLocator.Register(this);

        private void Start() => AdService.OnInitialized += CreateAndLoadAd;

        // Ad objects must be created only after init succeeds (LevelPlay requirement).
        private void CreateAndLoadAd()
        {
            _ad = new LevelPlayRewardedAd(RewardedAdUnitId);
            _ad.OnAdLoaded += _ => _loaded = true;
            // ponytail: no load-retry loop — a failed load logs and waits; the
            // next LoadAd happens after a shown ad closes.
            _ad.OnAdLoadFailed += error => Debug.LogWarning($"[Ads] rewarded load failed: {error}");
            _ad.OnAdRewarded += (_, _) => OnRewardGranted?.Invoke();
            _ad.OnAdClosed += _ =>
            {
                _loaded = false;
                _ad.LoadAd(); // preload the next one
            };
            _ad.LoadAd();
        }

        /// <summary>Shows the loaded rewarded ad; no-op when not ready.</summary>
        public void Show()
        {
            if (IsReady) _ad.ShowAd();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            AdService.OnInitialized -= CreateAndLoadAd;
            _ad?.DestroyAd();
        }
    }
}
