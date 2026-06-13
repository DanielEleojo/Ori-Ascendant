using OriAscendant.Audio;
using OriAscendant.Core;
using OriAscendant.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// Settings (GAMEPLAY §3.7): BGM/SFX toggles (PlayerPrefs via AudioPrefs —
    /// not SaveData), the honest cloud status line, version, and the entry to
    /// About &amp; Glossary. Opened from the header gear. Display + prefs only;
    /// never touches game state.
    /// </summary>
    public class SettingsScreenView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Toggle _bgmToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private TMP_Text _cloudStatus;
        [SerializeField] private TMP_Text _version;
        [SerializeField] private Button _aboutButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private AboutScreenView _about;

        private void Awake()
        {
            if (_bgmToggle != null) _bgmToggle.onValueChanged.AddListener(SetBgm);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.AddListener(SetSfx);
            if (_aboutButton != null) _aboutButton.onClick.AddListener(OpenAbout);
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
            if (_root != null) _root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_bgmToggle != null) _bgmToggle.onValueChanged.RemoveListener(SetBgm);
            if (_sfxToggle != null) _sfxToggle.onValueChanged.RemoveListener(SetSfx);
            if (_aboutButton != null) _aboutButton.onClick.RemoveListener(OpenAbout);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Hide);
        }

        public void Show()
        {
            if (_bgmToggle != null) _bgmToggle.SetIsOnWithoutNotify(AudioPrefs.BgmEnabled);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(AudioPrefs.SfxEnabled);
            if (_version != null) _version.text = "v" + Application.version;
            if (_cloudStatus != null)
            {
                _cloudStatus.text = ServiceLocator.TryGet(out CloudSaveManager cloud)
                    ? cloud.StatusLine
                    : "Local save only";
            }
            if (_root != null) _root.SetActive(true);
        }

        private void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void SetBgm(bool on) => AudioPrefs.BgmEnabled = on;

        private void SetSfx(bool on) => AudioPrefs.SfxEnabled = on;

        private void OpenAbout()
        {
            if (_about != null) _about.Show();
        }
    }
}
