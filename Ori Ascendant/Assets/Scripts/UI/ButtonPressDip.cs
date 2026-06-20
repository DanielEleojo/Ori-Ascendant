using UnityEngine;
using UnityEngine.EventSystems;

namespace OriAscendant.UI
{
    /// <summary>
    /// Visual press-dip for any button (issue #24 micro-feedback): scale dips to
    /// 0.96 on pointer down and eases back to 1.0 over 120 ms on release.
    /// Self-contained — attach to a Button GameObject; no external wiring needed.
    /// Reduce Motion: scale motion silenced, localScale stays at 1.0.
    /// </summary>
    public sealed class ButtonPressDip : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private const float DipTarget = 0.96f;
        private const float RecoverDuration = 0.12f;

        private bool _isPressed;
        // Sentinel: no active release yet — avoids a spurious first-frame recovery from 0.
        private float _releaseElapsed = float.MaxValue;

        private static bool ReduceMotion =>
            PlayerPrefs.GetInt("ReduceMotion", 0) != 0;

        private void Update()
        {
            if (!_isPressed)
                _releaseElapsed += Time.unscaledDeltaTime;

            float s;
            if (ReduceMotion)
                s = 1f;
            else if (_isPressed)
                s = DipTarget;
            else
                s = MotionHelper.PressDipScale(_releaseElapsed, RecoverDuration, false);

            transform.localScale = new Vector3(s, s, 1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            _releaseElapsed = 0f;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Release()
        {
            if (!_isPressed) return;
            _isPressed = false;
            _releaseElapsed = 0f;
        }
    }
}
