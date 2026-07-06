using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Self-animating "+N" feedback for channel taps (GAMEPLAY §5.3): rises and
    /// fades over its lifetime, then deactivates into a shared pool — rapid taps
    /// reuse instances instead of churning Instantiate/Destroy garbage.
    /// Reduce Motion: the rise (position motion) is silenced; the fade still runs.
    /// Pure presentation.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private const float ScatterPixels = 12f; // random ±x so rapid taps don't stack

        private static readonly List<FloatingText> s_pool = new List<FloatingText>();

        private TMP_Text _text;
        private float _age;
        private Vector2 _startPosition;
        private RectTransform _rect;

        public static void Spawn(RectTransform parent, Vector2 anchoredPosition, string message, Color color)
        {
            FloatingText floating = null;
            while (s_pool.Count > 0 && floating == null) // skips entries destroyed with an old parent
            {
                floating = s_pool[s_pool.Count - 1];
                s_pool.RemoveAt(s_pool.Count - 1);
            }

            if (floating == null)
            {
                var go = new GameObject("FloatingText", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(220f, 40f);

                var text = go.AddComponent<TextMeshProUGUI>();
                text.fontSize = 22f;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;

                floating = go.AddComponent<FloatingText>();
                floating._text = text;
                floating._rect = rect;
            }

            anchoredPosition.x += Random.Range(-ScatterPixels, ScatterPixels);
            floating._rect.SetParent(parent, false);
            floating._rect.anchoredPosition = anchoredPosition;
            floating._startPosition = anchoredPosition;
            floating._age = 0f;
            floating._text.text = message;
            floating._text.color = color;
            floating.gameObject.SetActive(true);
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / MotionScale.FloatingTextLifetime;
            if (t >= 1f)
            {
                gameObject.SetActive(false);
                s_pool.Add(this);
                return;
            }

            if (!MotionHelper.IsReduceMotion()) // rise is position motion — stays put under RM
                _rect.anchoredPosition = _startPosition + new Vector2(0f, MotionScale.FloatingTextRisePixels * t);
            var c = _text.color;
            c.a = 1f - t * t; // ease-out fade
            _text.color = c;
        }
    }
}
