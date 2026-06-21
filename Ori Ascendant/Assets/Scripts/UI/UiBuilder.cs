using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Shared UGUI construction helpers for the in-engine skins (ADR-0001). One home for
    /// "make a decorative child Image", the anchor/stretch/band helpers, and the deep
    /// transform search — so MainScreenSkin, ColdOpenSkin, and TitleScreenSkin stop each
    /// carrying their own drifted copies.
    /// </summary>
    public static class UiBuilder
    {
        /// <summary>A decorative child Image: stretched to fill its parent and
        /// non-raycasting (decorative skin art never eats touches). Callers that need
        /// different anchors, size, or a raycast target set them after creation.</summary>
        public static Image NewChildImage(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false; // decorative — never eat touches
            return img;
        }

        /// <summary>Name-first alias for <see cref="NewChildImage"/>, kept for call sites
        /// where the element name reads better first.</summary>
        public static Image NewStretchImage(string name, Transform parent) =>
            NewChildImage(parent, name);

        /// <summary>Anchor + size a rect at a normalised screen position.</summary>
        public static void PlaceAt(RectTransform rt, float ax, float ay, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(ax, ay);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>Horizontal-band anchor: near-full width, clamped to a y-band.</summary>
        public static void SetBand(RectTransform rt, float yMin, float yMax)
        {
            rt.anchorMin = new Vector2(0.05f, yMin);
            rt.anchorMax = new Vector2(0.95f, yMax);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Stretch a rect to fill its parent.</summary>
        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        /// <summary>Depth-first search by GameObject name.</summary>
        public static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>FindDeep + GetComponent in one call.</summary>
        public static T FindComp<T>(Transform root, string name) where T : Component
        {
            var t = FindDeep(root, name);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
