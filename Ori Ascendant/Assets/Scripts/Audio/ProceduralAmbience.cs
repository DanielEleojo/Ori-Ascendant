using OriAscendant.Core;
using UnityEngine;

namespace OriAscendant.Audio
{
    /// <summary>
    /// Builds an <see cref="AudioClip"/> from <see cref="AmbiencePad.Generate"/> at
    /// runtime and installs it as the default BGM theme via
    /// <see cref="AudioManager.InstallDefaultTheme"/>. Degrades silently if the
    /// AudioManager is not yet registered (safe in test scenes that never register it).
    ///
    /// Self-bootstraps via <see cref="Bootstrap"/> after scene load, matching the
    /// MainScreenSkin pattern — no scene wiring is required for the clip to appear.
    /// </summary>
    public sealed class ProceduralAmbience : MonoBehaviour
    {
        // ── TUNE-BY-EAR: generation parameters ──────────────────────────────
        // These pair with the frequency/amplitude constants in AmbiencePad.
        // 22050 Hz is sufficient for the drone frequency range and halves memory
        // usage vs 44100.  Increase to 44100 if you add higher-frequency content.
        private const int   SampleRate   = 22050;
        // 14 seconds gives ~0.07 Hz LFO one full cycle; increase to 28 s for an
        // even slower, more meditative swell.
        private const float LoopSeconds  = 14f;
        // ────────────────────────────────────────────────────────────────────

        private const string BootstrapCanvasName = "MainCanvas";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Only activate in scenes that carry the main canvas — keeps this inert
            // in EditMode/PlayMode test scenes with no audio infrastructure.
            if (FindMainCanvas() == null) return;
            // Skip if SceneBuilder already wired one into the scene (avoids double PCM alloc).
            if (FindObjectsByType<ProceduralAmbience>(FindObjectsSortMode.None).Length > 0) return;
            new GameObject(nameof(ProceduralAmbience)).AddComponent<ProceduralAmbience>();
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
            // Degrade silently on any failure, matching the rest of the audio system.
            try
            {
                InstallClip();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ProceduralAmbience] failed to install ambient clip: {e.Message}");
            }
        }

        private void InstallClip()
        {
            float[] pcm = AmbiencePad.Generate(SampleRate, LoopSeconds);
            int sampleCount = pcm.Length;

            // AudioClip.Create: name, lengthSamples, channels, frequency, stream.
            AudioClip clip = AudioClip.Create("ProceduralAmbiencePad", sampleCount,
                channels: 1, SampleRate, stream: false);
            clip.SetData(pcm, offsetSamples: 0);

            if (!ServiceLocator.TryGet(out AudioManager manager))
            {
                Debug.LogWarning("[ProceduralAmbience] AudioManager not registered yet — ambient clip not installed.");
                return;
            }

            manager.InstallDefaultTheme(clip);
        }
    }
}
