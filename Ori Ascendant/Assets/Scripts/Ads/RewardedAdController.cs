using System.Collections;
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
    /// Robustness (ported from Tribulation's AdsManager): a failed load retries
    /// after <see cref="LoadRetryDelay"/> so one flaky request can't leave the
    /// session ad-less; <see cref="ShowOrLoad"/> loads on demand when nothing is
    /// preloaded (UI can offer the ad unconditionally) and gives up after
    /// <see cref="OnDemandLoadTimeout"/> so callers never hang; a watchdog
    /// catches a ShowAd the SDK silently swallows; and a close without a reward
    /// holds <see cref="LateRewardWindow"/> because LevelPlay documents
    /// OnAdRewarded can arrive slightly AFTER OnAdClosed.
    ///
    /// SETUP: fill RewardedAdUnitId here and AppKey on AdService (which owns
    /// the single LevelPlay init).
    /// </summary>
    public sealed class RewardedAdController : MonoBehaviour
    {
        // Public ad identifier (not a secret). Fill from the LevelPlay dashboard.
        private static readonly string RewardedAdUnitId = "5nety1y03c5jc1cn";

        // Ad-plumbing timings (real seconds — unscaled; the ad overlay isn't
        // paused by Time.timeScale). SDK-behavior tuning, not gameplay balance,
        // so consts here rather than a ScriptableObject — same as Tribulation.
        private const float LoadRetryDelay = 6f;      // failed load → retry this much later
        private const float OnDemandLoadTimeout = 8f; // tap with nothing preloaded → give up after this
        private const float ShowWatchdogSeconds = 6f; // ShowAd that never displays → resolve failed
        private const float LateRewardWindow = 1.5f;  // reward may land just after close

        private LevelPlayRewardedAd _ad;
        private bool _loaded;
        private Coroutine _retryCo;     // pending load-retry (one at a time, never stacked)
        private Coroutine _onDemandCo;  // tap-time load-then-show in progress
        private Coroutine _watchdogCo;  // armed on ShowAd, disarmed on display/close/fail
        private Coroutine _lateRewardCo;
        private bool _rewardEarnedThisShow;

        // Pending ShowOrLoad failure callback. Cleared BEFORE invoking so every
        // resolution path (timeout, display-fail, watchdog, no-reward close) is
        // exactly-once — same guard as Tribulation's _pendingRevive.
        private System.Action _pendingFailed;

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
            _ad.OnAdLoadFailed += error =>
            {
                Debug.LogWarning($"[Ads] rewarded load failed: {error}");
                if (_retryCo == null) _retryCo = StartCoroutine(RetryLoad());
            };
            _ad.OnAdDisplayed += _ => CancelCo(ref _watchdogCo); // display proves the show wasn't swallowed
            _ad.OnAdRewarded += (_, _) =>
            {
                _rewardEarnedThisShow = true;
                OnRewardGranted?.Invoke();
            };
            _ad.OnAdDisplayFailed += (_, error) =>
            {
                Debug.LogWarning($"[Ads] rewarded display failed: {error}");
                CancelCo(ref _watchdogCo);
                _ad.LoadAd();
                FirePendingFailed(); // never leave the caller stuck on a failed show
            };
            _ad.OnAdClosed += _ =>
            {
                CancelCo(ref _watchdogCo);
                _loaded = false;
                _ad.LoadAd(); // preload the next one
                if (_rewardEarnedThisShow || _pendingFailed == null)
                    _pendingFailed = null; // rewarded (OnRewardGranted handled it) or nobody waiting
                else
                    _lateRewardCo = StartCoroutine(HoldForLateReward());
            };
            _ad.LoadAd();
        }

        private IEnumerator RetryLoad()
        {
            yield return new WaitForSecondsRealtime(LoadRetryDelay);
            _retryCo = null;
            if (!_loaded) _ad?.LoadAd();
        }

        // Bridges the OnAdRewarded-after-OnAdClosed race: keep the failure
        // resolution open a short while past close so a reward landing just
        // after still counts (OnRewardGranted has then already fired).
        private IEnumerator HoldForLateReward()
        {
            float t = 0f;
            while (t < LateRewardWindow)
            {
                if (_rewardEarnedThisShow)
                {
                    _pendingFailed = null;
                    _lateRewardCo = null;
                    yield break;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            _lateRewardCo = null;
            FirePendingFailed();
        }

        private IEnumerator ShowWatchdog()
        {
            yield return new WaitForSecondsRealtime(ShowWatchdogSeconds);
            _watchdogCo = null;
            Debug.LogWarning("[Ads] rewarded never displayed after ShowAd — resolving as failed.");
            _ad?.LoadAd();
            FirePendingFailed();
        }

        /// <summary>Shows the loaded rewarded ad; no-op when not ready.</summary>
        public void Show()
        {
            if (IsReady) _ad.ShowAd();
        }

        /// <summary>
        /// Shows the rewarded ad, loading on demand first when nothing is
        /// preloaded — so UI can offer the ad unconditionally. The reward (if
        /// earned) arrives via <see cref="OnRewardGranted"/>; onFailed fires
        /// exactly once when this attempt can no longer produce a reward: load
        /// timeout, display failure, swallowed show, or the ad closed without a
        /// reward (after the late-reward hold). One attempt at a time — calls
        /// while one is pending are ignored. A caller that loses interest
        /// (modal collected/closed) should call <see cref="CancelPendingShow"/>.
        /// </summary>
        public void ShowOrLoad(System.Action onFailed)
        {
            if (_ad == null) { onFailed?.Invoke(); return; } // init never succeeded
            if (_pendingFailed != null || _onDemandCo != null) return; // no re-entry
            _pendingFailed = onFailed ?? (() => { }); // non-null so it doubles as the in-flight flag
            _rewardEarnedThisShow = false;
            if (IsReady)
            {
                _ad.ShowAd();
                _watchdogCo = StartCoroutine(ShowWatchdog());
            }
            else
            {
                _onDemandCo = StartCoroutine(LoadThenShow());
            }
        }

        /// <summary>Abandons a pending <see cref="ShowOrLoad"/>: the onFailed
        /// callback is dropped, not fired. If the ad is already on screen this
        /// only silences the resolution — the ad itself runs its course.</summary>
        public void CancelPendingShow()
        {
            CancelCo(ref _onDemandCo);
            CancelCo(ref _lateRewardCo);
            _pendingFailed = null;
        }

        // Tap-time fallback when preloading hasn't produced an ad yet: load on
        // demand and show the moment it lands. The retry loop is parked
        // meanwhile so it can't issue a competing LoadAd.
        private IEnumerator LoadThenShow()
        {
            CancelCo(ref _retryCo);
            _ad.LoadAd();
            float t = 0f;
            while (t < OnDemandLoadTimeout)
            {
                if (_pendingFailed == null) { _onDemandCo = null; yield break; } // canceled
                if (_loaded)
                {
                    _onDemandCo = null;
                    _ad.ShowAd();
                    _watchdogCo = StartCoroutine(ShowWatchdog());
                    yield break;
                }
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            _onDemandCo = null;
            FirePendingFailed();
        }

        private void FirePendingFailed()
        {
            var cb = _pendingFailed;
            _pendingFailed = null; // clear first — guards against double-invoke
            cb?.Invoke();
        }

        private void CancelCo(ref Coroutine co)
        {
            if (co == null) return;
            StopCoroutine(co);
            co = null;
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister(this);
            AdService.OnInitialized -= CreateAndLoadAd;
            _ad?.DestroyAd();
        }
    }
}
