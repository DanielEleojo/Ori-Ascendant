using System;
using OriAscendant.Core;
using OriAscendant.Save;

namespace OriAscendant.Systems
{
    /// <summary>
    /// Offline Àṣẹ calculation, run on cold launch AND on resume-from-background
    /// (TECH_DESIGN §4). OFF-LIMITS CONSTRAINTS: pure math only — no
    /// Time.timeScale, no coroutines, no simulation. Reads the CACHED rate from
    /// SaveData (never recomputes it; AseGenerationSystem owns the rate).
    ///
    /// The two save-mutating entrypoints encode the two distinct intents
    /// (issue #17 / PRD #13 ⑥): first-launch initialization (no Àṣẹ credit;
    /// stamps generationStartTimestamp) and resume accrual (credits earned
    /// Àṣẹ; never touches generationStartTimestamp).
    /// </summary>
    public static class OfflineProgressCalculator
    {
        /// <summary>
        /// Hard cap on credited offline seconds — 8 hours. This is a locked
        /// business rule (CLAUDE.md / PRD §6), intrinsic to the calculation,
        /// not a tunable balance value.
        /// </summary>
        public const long MaxOfflineSeconds = 28800;

        /// <summary>Fired by <see cref="ApplyAccrual"/> after offline progress
        /// lands, so the UI can show the Welcome Back collect screen.
        /// First-launch initialization does NOT fire this. (earned, countedSeconds)</summary>
        public static event Action<BigNumber, long> OnOfflineProgressApplied;

        public readonly struct OfflineResult
        {
            public readonly BigNumber Earned;
            public readonly long CountedSeconds;
            public readonly bool IsFirstLaunch;

            public OfflineResult(BigNumber earned, long countedSeconds, bool isFirstLaunch)
            {
                Earned = earned;
                CountedSeconds = countedSeconds;
                IsFirstLaunch = isFirstLaunch;
            }
        }

        /// <summary>
        /// Pure computation: clamps elapsed to [0, 8h] and applies the path's
        /// offline rate modifier (1.0 when no path — paths modify the RATE,
        /// never the time cap).
        ///   earned = cachedRate × offlineRateModifier × clamp(now − last, 0, cap)
        /// Guards: lastSaveTimestamp == 0 is a fresh install (no prior session,
        /// no gain — without this, a new player would bank a free 8 hours);
        /// a future timestamp (clock skew, cloud merge) clamps to 0 so Àṣẹ can
        /// never regress.
        /// </summary>
        public static OfflineResult Compute(long lastSaveTimestamp, long nowUnix,
            BigNumber cachedRate, double offlineRateModifier)
        {
            if (lastSaveTimestamp == 0)
            {
                return new OfflineResult(BigNumber.Zero, 0, isFirstLaunch: true);
            }

            long elapsed = Math.Max(0, Math.Min(nowUnix - lastSaveTimestamp, MaxOfflineSeconds));
            if (elapsed == 0 || cachedRate.IsZero || offlineRateModifier <= 0.0)
            {
                return new OfflineResult(BigNumber.Zero, elapsed, isFirstLaunch: false);
            }

            BigNumber earned = cachedRate * offlineRateModifier * elapsed;
            return new OfflineResult(earned, elapsed, isFirstLaunch: false);
        }

        /// <summary>
        /// Cold-launch on a brand-new save: stamps both timestamps (lastSave
        /// AND generationStart) without crediting any Àṣẹ. Does NOT raise
        /// <see cref="OnOfflineProgressApplied"/> — there is no offline
        /// progress to welcome the player back to. The caller (game-lifecycle
        /// owner) is responsible for routing fresh saves here.
        /// </summary>
        public static void InitializeFirstLaunch(SaveData save, long nowUnix)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            save.lastSaveTimestamp = nowUnix;
            save.generationStartTimestamp = nowUnix;
        }

        /// <summary>
        /// Resume / cold-launch on an existing save: credits earned offline
        /// Àṣẹ, stamps lastSaveTimestamp ONLY (never generationStartTimestamp
        /// — the generation clock is owned by first-launch and the Tribulation
        /// resolve). Raises <see cref="OnOfflineProgressApplied"/> on success.
        /// Safe no-op on a fresh save: returns IsFirstLaunch=true and writes
        /// nothing, leaving routing to the caller.
        /// </summary>
        public static OfflineResult ApplyAccrual(SaveData save, long nowUnix, double offlineRateModifier)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            OfflineResult result = Compute(save.lastSaveTimestamp, nowUnix,
                save.GetAsePerSecond(), offlineRateModifier);

            if (result.IsFirstLaunch)
            {
                return result;
            }

            if (!result.Earned.IsZero)
            {
                save.SetAse(save.GetAse() + result.Earned);
            }
            save.lastSaveTimestamp = nowUnix;

            OnOfflineProgressApplied?.Invoke(result.Earned, result.CountedSeconds);
            return result;
        }
    }
}
