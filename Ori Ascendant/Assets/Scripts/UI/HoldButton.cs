using UnityEngine;
using UnityEngine.EventSystems;

namespace OriAscendant.UI
{
    /// <summary>
    /// Pointer-hold state for the hold-to-begin ritual commitment (GAMEPLAY §3.5
    /// confirm sheet). The owner polls <see cref="IsHeld"/> and times the hold —
    /// this component only tracks contact, including cancel-on-exit.
    /// </summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData) => IsHeld = true;

        public void OnPointerUp(PointerEventData eventData) => IsHeld = false;

        public void OnPointerExit(PointerEventData eventData) => IsHeld = false;
    }
}
