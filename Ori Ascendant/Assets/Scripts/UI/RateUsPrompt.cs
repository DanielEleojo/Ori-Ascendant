using OriAscendant.Core;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using UnityEngine;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace OriAscendant.UI
{
    /// <summary>
    /// Fires the native "rate this app" prompt exactly once, at the end of the
    /// player's first-ever completed Crossing (ascend or fall — both are a real
    /// first-Crossing milestone per this game's "never a dead end" philosophy).
    ///
    /// Two-phase: <see cref="TribulationSystem.OnTribulationComplete"/> fires while
    /// the ceremony overlay is still opaque (too early for a native dialog), so this
    /// only stashes the fact that the milestone happened. The actual
    /// <see cref="Device.RequestStoreReview"/> call waits for
    /// <see cref="TribulationScreen.OnCeremonyClosed"/> — the moment the overlay is
    /// fully down and it's safe to layer a system dialog on top of the game.
    ///
    /// Self-bootstraps via <see cref="Bootstrap"/> after scene load, mirroring
    /// ProceduralAmbience — degrades silently if the systems it needs aren't
    /// registered (safe in EditMode/PlayMode test scenes).
    /// </summary>
    public sealed class RateUsPrompt : MonoBehaviour
    {
        // ponytail: single-consumer flag, not worth a shared Prefs class for one bool.
        private const string ShownPrefsKey = "ori_rateus_shown";

        private const string BootstrapCanvasName = "MainCanvas";

        private TribulationSystem _tribulation;
        private TribulationScreen _tribulationScreen;
        private SaveManager _saveManager;
        private bool _pendingFirstCrossing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Only activate in scenes that carry the main canvas — keeps this inert
            // in EditMode/PlayMode test scenes with no gameplay infrastructure.
            if (FindMainCanvas() == null) return;
            if (FindObjectsByType<RateUsPrompt>(FindObjectsSortMode.None).Length > 0) return;
            var go = new GameObject(nameof(RateUsPrompt));
            go.AddComponent<RateUsPrompt>();
            DontDestroyOnLoad(go);
        }

        private static Canvas FindMainCanvas()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
                if (c.name == BootstrapCanvasName) return c;
            return null;
        }

        private void Start()
        {
            // Degrade silently if any dependency isn't registered — matches the
            // rest of this codebase's "safe in test scenes" philosophy.
            if (!ServiceLocator.TryGet(out _tribulation)) return;
            if (!ServiceLocator.TryGet(out _tribulationScreen)) return;
            if (!ServiceLocator.TryGet(out _saveManager)) return;

            _tribulation.OnTribulationComplete += HandleTribulationComplete;
            _tribulationScreen.OnCeremonyClosed += HandleCeremonyClosed;
        }

        private void OnDestroy()
        {
            if (_tribulation != null) _tribulation.OnTribulationComplete -= HandleTribulationComplete;
            if (_tribulationScreen != null) _tribulationScreen.OnCeremonyClosed -= HandleCeremonyClosed;
        }

        private void HandleTribulationComplete(bool ascended, AncestorData ancestor)
        {
            // generationCount is incremented before this event fires (TribulationSystem
            // .CommitAtomicWrite), so == 1 here means the Crossing that just resolved
            // was the player's first ever, for either outcome.
            _pendingFirstCrossing = _saveManager.Current.lineage.generationCount == 1;
        }

        private void HandleCeremonyClosed()
        {
            if (!_pendingFirstCrossing) return;
            _pendingFirstCrossing = false;

            if (PlayerPrefs.GetInt(ShownPrefsKey, 0) != 0) return;

#if UNITY_IOS
            Device.RequestStoreReview();
#endif
            PlayerPrefs.SetInt(ShownPrefsKey, 1);
            PlayerPrefs.Save();
        }
    }
}
