using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI.Screens
{
    /// <summary>
    /// The climb-tied Crossroads modal (DYNASTY_REDESIGN slice 2a). A blocking gate
    /// like the Ori vow: the dim swallows input until a choice is made, no dismiss,
    /// re-Shown by MainScreenController while CrossroadsSystem.HasPending. One-tap
    /// commit — each option button calls CrossroadsSystem.Choose(i) directly (no
    /// select-then-confirm; a crossroads choice is meant to land with weight). The
    /// prompt and the active option set are rebuilt on every Show because the
    /// PendingBeat differs each stage-advance. UI never writes state: it calls
    /// Choose. Mirrors OriScreenView.
    /// </summary>
    public class CrossroadsModalView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _promptText;
        [SerializeField] private Button[] _optionButtons;   // fixed 4 (seed: one option per Ori)
        [SerializeField] private TMP_Text[] _optionLabels;  // index-aligned with _optionButtons

        private CrossroadsSystem _crossroads;

        private void Awake()
        {
            if (_root != null) _root.SetActive(false);
        }

        public void Show()
        {
            _crossroads = ServiceLocator.Get<CrossroadsSystem>();
            CrossroadsBeat beat = _crossroads.PendingBeat;
            if (beat == null) return; // defensive: nothing to present

            if (_promptText != null) _promptText.text = beat.prompt;

            int optionCount = beat.options != null ? beat.options.Length : 0;
            for (int i = 0; i < _optionButtons.Length; i++)
            {
                bool used = i < optionCount;
                if (_optionButtons[i] != null) _optionButtons[i].gameObject.SetActive(used);
                if (!used) continue;

                if (_optionLabels[i] != null) _optionLabels[i].text = beat.options[i].text;

                int index = i; // capture a fresh local — closures bind the variable, not its value
                _optionButtons[i].onClick.RemoveAllListeners(); // re-bind per beat
                _optionButtons[i].onClick.AddListener(() => Choose(index));
            }

            if (_root != null) _root.SetActive(true);
        }

        private void Choose(int optionIndex)
        {
            if (_crossroads == null || !_crossroads.Choose(optionIndex)) return;
            if (_crossroads.HasPending) Show();              // present the next queued crossroads
            else if (_root != null) _root.SetActive(false);  // queue drained — close
        }
    }
}
