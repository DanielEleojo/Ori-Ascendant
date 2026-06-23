using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Shared selection-ring + lift logic for the three selectable card views
    /// (PathCardView, OriCardView, CrossroadsOptionView).
    ///
    /// Extracted here to remove triplication — Unit 3 of the UI-cohesion pass.
    /// Each card view still owns its own text fields and Bind() method; only the
    /// purely-visual selected-state wiring lives here.
    ///
    /// Stateless construction helpers + stateful Apply that drives the card's
    /// Image and child ring each frame the selection state changes.
    /// </summary>
    public static class CardSelectionVisual
    {
        // ponytail: fixed texture size — 9-sliced so the actual card can be any size.
        private const int TexSize = 48;

        /// <summary>
        /// Build the background sprite once and assign it to <paramref name="bg"/>.
        /// Call from Awake (or the first Bind call before any other work).
        /// </summary>
        public static void InitBackground(Image bg)
        {
            if (bg == null) return;
            bg.sprite = ProceduralSprites.RoundedRect(TexSize, CornerRadiusSpec.Card);
            bg.type   = Image.Type.Sliced;
        }

        /// <summary>
        /// Create the gold selection ring as a hidden child of <paramref name="parent"/>,
        /// stretched over the card.  Returns the ring Image; the caller stores it.
        /// Safe to call multiple times — returns the existing ring if it was already created.
        /// </summary>
        public static Image CreateRing(Transform parent)
        {
            // Guard: reuse if already built (Bind called again on the same object).
            var existing = parent.Find("_SelectionRing");
            if (existing != null) return existing.GetComponent<Image>();

            var go = new GameObject("_SelectionRing");
            go.transform.SetParent(parent, worldPositionStays: false);

            var ring = go.AddComponent<Image>();
            ring.sprite         = ProceduralSprites.RoundedBorder(
                TexSize, CornerRadiusSpec.Card, CornerRadiusSpec.BorderStroke);
            ring.type           = Image.Type.Sliced;
            ring.color          = CardViewSpec.SelectedRing;
            ring.raycastTarget  = false;

            // Stretch to fill the card exactly.
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.SetActive(false);
            return ring;
        }

        /// <summary>
        /// Drive the card's visual state.  Call from SetSelected(bool).
        /// <paramref name="primaryTexts"/> may be null or empty — any non-null entry is coloured.
        /// </summary>
        public static void Apply(
            bool selected,
            Transform card,
            Image bg,
            Image ring,
            UnityEngine.UI.Graphic[] primaryTexts)
        {
            if (bg != null)
                bg.color = selected ? CardViewSpec.Selected : CardViewSpec.Idle;

            if (ring != null)
                ring.gameObject.SetActive(selected);

            card.localScale = selected
                ? Vector3.one * CardViewSpec.SelectedScale
                : Vector3.one;

            if (primaryTexts != null)
            {
                Color textColor = selected ? CardViewSpec.SelectedText : CardViewSpec.IdleText;
                foreach (var t in primaryTexts)
                {
                    if (t != null) t.color = textColor;
                }
            }
        }
    }
}
