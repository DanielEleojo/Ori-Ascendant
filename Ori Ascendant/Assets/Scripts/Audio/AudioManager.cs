using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using UnityEngine;

namespace OriAscendant.Audio
{
    /// <summary>
    /// BGM + SFX + haptics (TECH_DESIGN §4). Crossfades BGM themes on path
    /// change, plays a stinger on the Crossing, and fires one-shot SFX on
    /// progression events. ALL clip slots are SerializeField and may be null —
    /// every play is null-guarded, so the system is fully functional before
    /// Daniel's audio assets land (they slot straight into these fields).
    /// Subscribes in Start (systems register in their Awakes).
    /// Haptic routing delegates to HapticRouter (issue #21).
    /// Also: ducks the bed under the ceremony sting, muffles it with storm
    /// tension near the tribulation (SetTribulationTension), and pauses the
    /// BGM sources while the app is backgrounded.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("BGM (index: 0 default, 1 Ane, 2 Sango, 3 Osun)")]
        [SerializeField] private AudioClip[] _bgmThemes = new AudioClip[AudioTrackSelector.ThemeCount];
        [SerializeField] private float _crossfadeSeconds = 1.5f;
        [SerializeField] private float _bgmVolume = 0.6f;

        [Header("Stingers / SFX")]
        [SerializeField] private AudioClip _tribulationStinger;
        [SerializeField] private AudioClip _sfxAdvance;
        [SerializeField] private AudioClip _sfxChannel;
        [SerializeField] private AudioClip _sfxCollect;
        [SerializeField] private AudioClip _sfxAscend;
        [SerializeField] private AudioClip _sfxFall;

        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private AudioSource _sfx;
        private bool _bgmAIsActive = true;
        private readonly AudioCrossfade _crossfade = new AudioCrossfade();

        // Ceremony-sting ducking (no mixer): the bed dips to DuckLevel when the
        // stinger fires and recovers linearly over DuckRecoverSeconds (in Update).
        private const float DuckLevel = 0.4f;
        private const float DuckRecoverSeconds = 0.5f;
        private float _duckRemaining;

        // Tribulation storm tension (GAMEPLAY §3.5 buildup): one low-pass cutoff
        // lerp on the BGM sources. MainScreenSkin pushes the level in from
        // TribulationAtmosphere.TensionLevel — UI owns the canonical fractions and
        // the asmdef direction is UI → Audio, so it cannot be referenced from here.
        private const float LowPassOpenHz = 22000f; // fully open — filter idle
        private const float LowPassTenseHz = 1200f; // full storm pressure at the vignette fraction
        private AudioLowPassFilter _lowPassA;
        private AudioLowPassFilter _lowPassB;
        private float _appliedTension;

        private bool _bgmPausedByApp;

        private IHapticFeedback _haptics;
        private CultivationSystem _cultivation;
        private TribulationSystem _tribulation;
        private AseGenerationSystem _aseGen;
        private AncestralCouncilSystem _council;
        private MarketplaceSystem _marketplace;

        private void Awake()
        {
            _bgmA = CreateSource("BGM_A", loop: true);
            _bgmB = CreateSource("BGM_B", loop: true);
            _sfx = CreateSource("SFX", loop: false);
            _lowPassA = AddLowPass(_bgmA);
            _lowPassB = AddLowPass(_bgmB);
#if UNITY_IOS && !UNITY_EDITOR
            _haptics = new GatedHaptics(new iOSHaptics());
#else
            _haptics = new GatedHaptics(new NullHaptics());
#endif
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet(out _cultivation))
            {
                _cultivation.OnPathChosen += HandlePathChosen;
                _cultivation.OnStageAdvanced += HandleStageAdvanced;
            }
            if (ServiceLocator.TryGet(out _tribulation))
            {
                _tribulation.OnTribulationComplete += HandleTribulationComplete;
            }
            if (ServiceLocator.TryGet(out _aseGen))
            {
                _aseGen.OnAseChanneled += HandleChanneled;
            }
            if (ServiceLocator.TryGet(out _council))
            {
                _council.OnAncestorAdded += HandleAncestorAdded;
            }
            if (ServiceLocator.TryGet(out _marketplace))
            {
                _marketplace.OnContestResolved += HandleContestResolved;
            }
            OfflineProgressCalculator.OnOfflineProgressApplied += HandleOfflineCollected;

            PlayTheme(AudioTrackSelector.ThemeIndexForPath(-1), immediate: true);
        }

        private void OnDestroy()
        {
            if (_cultivation != null)
            {
                _cultivation.OnPathChosen -= HandlePathChosen;
                _cultivation.OnStageAdvanced -= HandleStageAdvanced;
            }
            if (_tribulation != null) _tribulation.OnTribulationComplete -= HandleTribulationComplete;
            if (_aseGen != null) _aseGen.OnAseChanneled -= HandleChanneled;
            if (_council != null) _council.OnAncestorAdded -= HandleAncestorAdded;
            if (_marketplace != null) _marketplace.OnContestResolved -= HandleContestResolved;
            OfflineProgressCalculator.OnOfflineProgressApplied -= HandleOfflineCollected;
            ServiceLocator.Unregister(this);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Sting duck: dip is instant, recovery linear over DuckRecoverSeconds.
            if (_duckRemaining > 0f) _duckRemaining = Mathf.Max(0f, _duckRemaining - dt);
            float duck = Mathf.Lerp(1f, DuckLevel, _duckRemaining / DuckRecoverSeconds);
            float master = (AudioPrefs.BgmEnabled ? _bgmVolume : 0f) * duck;

            AudioSource active = _bgmAIsActive ? _bgmA : _bgmB;   // fade target / current bed
            AudioSource inactive = _bgmAIsActive ? _bgmB : _bgmA; // fade source

            if (_crossfade.IsFading)
            {
                _crossfade.Tick(dt);
                active.volume = _crossfade.IncomingVolume * master;
                inactive.volume = _crossfade.OutgoingVolume * master;
                if (!_crossfade.IsFading) inactive.Stop();
            }
            else
            {
                active.volume = master; // keeps the BGM toggle + duck live between fades
            }
        }

        // App lifecycle: silence the bed while backgrounded. iOS fires both Pause and
        // Focus around a background/return — the flag makes the pair idempotent.
        private void OnApplicationPause(bool paused) => SetBgmPausedByApp(paused);
        private void OnApplicationFocus(bool hasFocus) => SetBgmPausedByApp(!hasFocus);

        private void SetBgmPausedByApp(bool paused)
        {
            if (_bgmA == null || _bgmB == null || paused == _bgmPausedByApp) return;
            _bgmPausedByApp = paused;
            if (paused) { _bgmA.Pause(); _bgmB.Pause(); }
            else { _bgmA.UnPause(); _bgmB.UnPause(); }
        }

        // ---- event handlers ----

        private void HandlePathChosen(int pathIndex) =>
            PlayTheme(AudioTrackSelector.ThemeIndexForPath(pathIndex), immediate: false);

        private void HandleStageAdvanced(int _)
        {
            PlaySfx(_sfxAdvance);
            HapticRouter.RouteStageAdvanced(_haptics);
        }

        private void HandleTribulationComplete(bool didAscend, AncestorData _)
        {
            PlaySfx(_tribulationStinger);
            PlaySfx(didAscend ? _sfxAscend : _sfxFall);
            // Duck the bed so the sting punches through — only when one actually
            // played (clips may not have landed yet / SFX may be muted).
            if (AudioPrefs.SfxEnabled && (_tribulationStinger != null || (didAscend ? _sfxAscend : _sfxFall) != null))
                _duckRemaining = DuckRecoverSeconds;
            HapticRouter.RouteTribulationComplete(_haptics, didAscend);
        }

        private void HandleChanneled(Core.BigNumber _)
        {
            PlaySfx(_sfxChannel);
            HapticRouter.RouteChanneled(_haptics);
        }

        private void HandleAncestorAdded(Save.AncestorData _) =>
            HapticRouter.RouteAncestorStarIgnite(_haptics);

        // No contest SFX asset exists yet — feedback is haptic-only (issue #38).
        private void HandleContestResolved(ContestOutcome outcome) =>
            HapticRouter.RouteContestResolved(_haptics, outcome.Won);

        private void HandleOfflineCollected(Core.BigNumber earned, long seconds)
        {
            if (!earned.IsZero) PlaySfx(_sfxCollect);
        }

        /// <summary>Selection tick for UI button presses. Call from UI components
        /// that lack a corresponding game-event (e.g. settings, navigation buttons).
        /// Channel tap and stage advance already route via their own game events.</summary>
        public void PlaySelect() => _haptics.Select();

        /// <summary>Re-applies the BGM volume to the live source immediately — the
        /// settings toggle calls this so muting takes effect without waiting for the
        /// next theme change (Update keeps it applied thereafter).</summary>
        public void ApplyBgmVolume()
        {
            AudioSource active = _bgmAIsActive ? _bgmA : _bgmB;
            if (active == null || _crossfade.IsFading) return; // mid-fade: Update folds the new master in next tick
            active.volume = AudioPrefs.BgmEnabled ? _bgmVolume : 0f;
        }

        /// <summary>Storm pressure on the BGM while the final stage approaches the
        /// tribulation: one low-pass cutoff lerp, fully open at 0 and muffled at 1.
        /// MainScreenSkin pushes TribulationAtmosphere.TensionLevel here each frame.
        /// Stateless — applies the current level directly, so a resume or a stage
        /// reset jumps straight to the right state and never replays intermediates.</summary>
        public void SetTribulationTension(float tension01)
        {
            float t = Mathf.Clamp01(tension01);
            if (Mathf.Approximately(t, _appliedTension)) return;
            _appliedTension = t;

            bool tense = t > 0f;
            float cutoff = Mathf.Lerp(LowPassOpenHz, LowPassTenseHz, t);
            if (_lowPassA != null) { _lowPassA.enabled = tense; _lowPassA.cutoffFrequency = cutoff; }
            if (_lowPassB != null) { _lowPassB.enabled = tense; _lowPassB.cutoffFrequency = cutoff; }
        }

        // ---- playback ----

        private void PlayTheme(int themeIndex, bool immediate)
        {
            AudioClip clip = themeIndex >= 0 && themeIndex < _bgmThemes.Length ? _bgmThemes[themeIndex] : null;

            // Path theme slots ship null until the assets land: switching to a null
            // clip would crossfade the current bed out to permanent silence — keep
            // whatever is playing (the slot-0 ambient) and skip the switch instead.
            if (clip == null) return;

            float master = AudioPrefs.BgmEnabled ? _bgmVolume : 0f;

            AudioSource incoming = _bgmAIsActive ? _bgmB : _bgmA; // swap targets
            AudioSource outgoing = _bgmAIsActive ? _bgmA : _bgmB;
            _bgmAIsActive = !_bgmAIsActive;

            incoming.clip = clip;
            incoming.volume = immediate ? master : 0f;
            if (clip != null) incoming.Play();

            if (immediate)
            {
                outgoing.Stop();
                _crossfade.Tick(float.MaxValue); // ensure not mid-fade
            }
            else
            {
                _crossfade.Begin(_crossfadeSeconds);
            }
        }

        /// <summary>Installs a runtime-generated clip as the default (path-less) BGM
        /// theme and plays it if the player is currently on the default theme. Lets
        /// <see cref="ProceduralAmbience"/> supply the ambient bed without a serialised
        /// asset. Respects <see cref="AudioPrefs.BgmEnabled"/> + <see cref="_bgmVolume"/>.
        /// No-op if <paramref name="clip"/> is null.</summary>
        public void InstallDefaultTheme(AudioClip clip)
        {
            if (clip == null) return;
            _bgmThemes[0] = clip;
            // If we are currently path-less (default theme slot 0 is active), replay
            // so the new clip starts immediately.  PlayTheme is the existing crossfade
            // path — passing immediate:true matches the Start() boot so there is no
            // fade-in on the very first load.
            int currentSlot = AudioTrackSelector.ThemeIndexForPath(-1); // 0
            AudioSource active = _bgmAIsActive ? _bgmA : _bgmB;
            bool isDefaultPlaying = active.clip == null || active.clip == clip ||
                                    currentSlot == 0;
            if (isDefaultPlaying)
                PlayTheme(currentSlot, immediate: true);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || !AudioPrefs.SfxEnabled || _sfx == null) return;
            _sfx.PlayOneShot(clip);
        }

        private AudioSource CreateSource(string name, bool loop)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.volume = 0f;
            return source;
        }

        private AudioLowPassFilter AddLowPass(AudioSource source)
        {
            var filter = source.gameObject.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = LowPassOpenHz;
            filter.enabled = false; // idle until SetTribulationTension pushes it
            return filter;
        }
    }
}
