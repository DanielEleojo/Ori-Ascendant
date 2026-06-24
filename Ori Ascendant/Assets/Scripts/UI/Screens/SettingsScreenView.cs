using OriAscendant.Audio;
using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// Settings (issue #31 / GAMEPLAY §3.7): grouped hairline rows.
    ///   Sound  — BGM / SFX toggles
    ///   Feel   — Haptics on/off
    ///   Motion — Reduce Motion on/off (in-app override; iOS native bridge writes the
    ///            same key per ADR-0004)
    ///   Account — Game Center / cloud status (silent-fallback aware)
    ///   About  — credits + cultural attribution
    ///
    /// All preferences written to PlayerPrefs, NOT SaveData — no migration version bump.
    /// Display + prefs only; never touches game state.
    /// </summary>
    public class SettingsScreenView : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private Toggle _bgmToggle;
        [SerializeField] private Toggle _sfxToggle;

        [Header("Feel")]
        [SerializeField] private Toggle _hapticsToggle;

        [Header("Motion")]
        [SerializeField] private Toggle _reduceMotionToggle;

        [Header("Account")]
        [SerializeField] private TMP_Text _cloudStatus;

        [Header("Footer")]
        [SerializeField] private TMP_Text _version;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _closeButton;

        [Header("Panels")]
        [SerializeField] private GameObject _root;
        [SerializeField] private AboutScreenView _about;

        private CanvasGroup _canvasGroup;
        private OverlayTransition _transition;

        private void Awake()
        {
            if (_bgmToggle != null)          _bgmToggle.onValueChanged.AddListener(SetBgm);
            if (_sfxToggle != null)          _sfxToggle.onValueChanged.AddListener(SetSfx);
            if (_hapticsToggle != null)      _hapticsToggle.onValueChanged.AddListener(SetHaptics);
            if (_reduceMotionToggle != null) _reduceMotionToggle.onValueChanged.AddListener(SetReduceMotion);
            if (_aboutButton != null)        _aboutButton.onClick.AddListener(OpenAbout);
            if (_closeButton != null)        _closeButton.onClick.AddListener(Hide);
            if (_root != null)
            {
                _root.SetActive(false);
                _canvasGroup = _root.GetComponent<CanvasGroup>() ?? _root.AddComponent<CanvasGroup>();
            }
            MotionPrefs.SyncOsFlag(); // mirror OS Reduce-Motion into PlayerPrefs on startup (ADR-0004 / #5)
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus) MotionPrefs.SyncOsFlag(); // re-sync if user changed OS setting while app was backgrounded
        }

        private void OnDestroy()
        {
            if (_bgmToggle != null)          _bgmToggle.onValueChanged.RemoveListener(SetBgm);
            if (_sfxToggle != null)          _sfxToggle.onValueChanged.RemoveListener(SetSfx);
            if (_hapticsToggle != null)      _hapticsToggle.onValueChanged.RemoveListener(SetHaptics);
            if (_reduceMotionToggle != null) _reduceMotionToggle.onValueChanged.RemoveListener(SetReduceMotion);
            if (_aboutButton != null)        _aboutButton.onClick.RemoveListener(OpenAbout);
            if (_closeButton != null)        _closeButton.onClick.RemoveListener(Hide);
        }

        private void Update()
        {
            if (_root == null || !_root.activeSelf) return;
            if (_transition.TickAndApply(_canvasGroup, _root.transform, Time.unscaledDeltaTime, MotionHelper.IsReduceMotion()))
                _root.SetActive(false);
        }

        public void Show()
        {
            if (_bgmToggle != null)          _bgmToggle.SetIsOnWithoutNotify(AudioPrefs.BgmEnabled);
            if (_sfxToggle != null)          _sfxToggle.SetIsOnWithoutNotify(AudioPrefs.SfxEnabled);
            if (_hapticsToggle != null)      _hapticsToggle.SetIsOnWithoutNotify(HapticPrefs.HapticsEnabled);
            if (_reduceMotionToggle != null) _reduceMotionToggle.SetIsOnWithoutNotify(MotionPrefs.ReduceMotionEnabled);
            if (_version != null)            _version.text = "v" + Application.version;
            if (_cloudStatus != null)
            {
                _cloudStatus.text = ServiceLocator.TryGet(out CloudSaveManager cloud)
                    ? cloud.StatusLine
                    : "Local save only";
            }
            if (_root != null) _root.SetActive(true);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _transition.Open();
        }

        private void Hide()
        {
            _transition.Close();
        }

        private void SetBgm(bool on)          => AudioPrefs.BgmEnabled = on;
        private void SetSfx(bool on)          => AudioPrefs.SfxEnabled = on;
        private void SetHaptics(bool on)      => HapticPrefs.HapticsEnabled = on;
        private void SetReduceMotion(bool on) => MotionPrefs.ReduceMotionEnabled = on;

        private void OpenAbout()
        {
            if (_about != null) _about.Show();
        }
    }
}
