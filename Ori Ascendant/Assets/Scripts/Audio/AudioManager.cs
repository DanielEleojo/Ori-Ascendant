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

        private IHapticFeedback _haptics;
        private CultivationSystem _cultivation;
        private TribulationSystem _tribulation;
        private AseGenerationSystem _aseGen;

        private void Awake()
        {
            _bgmA = CreateSource("BGM_A", loop: true);
            _bgmB = CreateSource("BGM_B", loop: true);
            _sfx = CreateSource("SFX", loop: false);
#if UNITY_IOS && !UNITY_EDITOR
            _haptics = new DeviceHaptics();
#else
            _haptics = new NullHaptics();
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
            OfflineProgressCalculator.OnOfflineProgressApplied -= HandleOfflineCollected;
            ServiceLocator.Unregister(this);
        }

        private void Update()
        {
            if (!_crossfade.IsFading) return;

            _crossfade.Tick(Time.unscaledDeltaTime);
            float master = AudioPrefs.BgmEnabled ? _bgmVolume : 0f;
            AudioSource incoming = _bgmAIsActive ? _bgmA : _bgmB;
            AudioSource outgoing = _bgmAIsActive ? _bgmB : _bgmA;
            incoming.volume = _crossfade.IncomingVolume * master;
            outgoing.volume = _crossfade.OutgoingVolume * master;
            if (!_crossfade.IsFading) outgoing.Stop();
        }

        // ---- event handlers ----

        private void HandlePathChosen(int pathIndex) =>
            PlayTheme(AudioTrackSelector.ThemeIndexForPath(pathIndex), immediate: false);

        private void HandleStageAdvanced(int _)
        {
            PlaySfx(_sfxAdvance);
            _haptics.Medium();
        }

        private void HandleTribulationComplete(bool didAscend, AncestorData _)
        {
            PlaySfx(_tribulationStinger);
            PlaySfx(didAscend ? _sfxAscend : _sfxFall);
            _haptics.Heavy();
        }

        private void HandleChanneled(Core.BigNumber _)
        {
            PlaySfx(_sfxChannel);
            _haptics.Light();
        }

        private void HandleOfflineCollected(Core.BigNumber earned, long seconds)
        {
            if (!earned.IsZero) PlaySfx(_sfxCollect);
        }

        // ---- playback ----

        private void PlayTheme(int themeIndex, bool immediate)
        {
            AudioClip clip = themeIndex >= 0 && themeIndex < _bgmThemes.Length ? _bgmThemes[themeIndex] : null;
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
    }
}
