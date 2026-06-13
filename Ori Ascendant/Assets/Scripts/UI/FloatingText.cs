using TMPro;
using UnityEngine;

namespace OriAscendant.UI
{
    /// <summary>
    /// Self-animating "+N" feedback for channel taps (GAMEPLAY §5.3): rises and
    /// fades over its lifetime, then destroys itself. Pure presentation.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        private const float LifetimeSeconds = 0.8f;
        private const float RisePixels = 70f;

        private TMP_Text _text;
        private float _age;
        private Vector2 _startPosition;
        private RectTransform _rect;

        public static void Spawn(RectTransform parent, Vector2 anchoredPosition, string message, Color color)
        {
            var go = new GameObject("FloatingText", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 40f);
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.text = message;

            var floating = go.AddComponent<FloatingText>();
            floating._text = text;
            floating._rect = rect;
            floating._startPosition = anchoredPosition;
        }

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = _age / LifetimeSeconds;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            _rect.anchoredPosition = _startPosition + new Vector2(0f, RisePixels * t);
            var c = _text.color;
            c.a = 1f - t * t; // ease-out fade
            _text.color = c;
        }
    }
}
