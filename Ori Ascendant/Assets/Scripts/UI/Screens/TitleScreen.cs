using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// First-launch surface (GAMEPLAY §3.1): title, the framing proverb, and a
    /// full-screen "Touch to begin". One tap dismisses it — that is the entire
    /// onboarding (tutorial flow is an explicit PRD non-goal). The idle loop
    /// already runs underneath.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _beginButton;

        private void Awake()
        {
            if (_beginButton != null) _beginButton.onClick.AddListener(Dismiss);
        }

        private void OnDestroy()
        {
            if (_beginButton != null) _beginButton.onClick.RemoveListener(Dismiss);
        }

        private void Dismiss()
        {
            if (_root != null) _root.SetActive(false);
        }
    }
}
