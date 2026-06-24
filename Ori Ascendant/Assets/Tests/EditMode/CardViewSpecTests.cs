using NUnit.Framework;
using OriAscendant.Data;
using OriAscendant.UI;
using OriAscendant.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.Tests.EditMode
{
    /// <summary>
    /// Gate: card-view selected state — background color, scale lift, and ring
    /// visibility after Unit 3 of the UI-cohesion pass.
    ///
    /// MonoBehaviour tests: instantiate each view on a real GameObject, wire
    /// _background via SerializedObject (EditModeTestHelpers.Inject), call Bind
    /// (which sets the private _ring field via the guard in Bind), then assert
    /// the visual outcomes of SetSelected.
    ///
    /// Why Bind instead of just CreateRing manually:
    ///   AddComponent does NOT trigger Awake in EditMode, so _ring stays null on the
    ///   MonoBehaviour. Calling CreateRing from the test creates the child GO but
    ///   never sets the view's _ring field — Apply would get null and skip SetActive.
    ///   Calling Bind triggers the `if (_ring == null) _ring = CreateRing(...)` guard
    ///   inside each view, which does set the private field. Bind is the right seam.
    ///
    /// Note: ProceduralSprites builds Texture2D/Sprite in the editor without Play
    /// mode — safe headlessly, but Sprite/Texture2D are NOT destroyed by
    /// DestroyImmediate(root) and accumulate as leaked assets across the test run.
    /// This is acceptable for an EditMode gate (Unity clears leaked assets on domain
    /// reload); a future improvement could track and Object.DestroyImmediate them.
    /// </summary>
    public class CardViewSpecTests
    {
        private GameObject _root;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("CardViewSpec_TestRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Build a card view with its _background wired via SerializedObject, then
        /// call the view's own Bind() so the view's private _ring field is populated
        /// through the in-view guard `if (_ring == null) _ring = CreateRing(...)`.
        /// Each overload calls the correct Bind signature for that view type.
        /// </summary>
        private PathCardView MakePathCard()
        {
            var (view, bg) = BuildBase<PathCardView>();
            var stub = EditModeTestHelpers.MakePath("TestPath", 1.0, 1.0, 1.0);
            view.Bind(stub, 0, _ => { });
            return view;
        }

        private OriCardView MakeOriCard()
        {
            var (view, bg) = BuildBase<OriCardView>();
            var stub = new OriVirtue { virtueName = "TestVirtue", vowLine = "Test vow." };
            view.Bind(stub, 0, _ => { });
            return view;
        }

        private CrossroadsOptionView MakeCrossroadsCard()
        {
            var (view, bg) = BuildBase<CrossroadsOptionView>();
            var stub = new CrossroadsOption { optionText = "Test option" };
            view.Bind(stub, 0, _ => { });
            return view;
        }

        private (T view, Image bg) BuildBase<T>() where T : MonoBehaviour
        {
            var go = new GameObject(typeof(T).Name);
            go.transform.SetParent(_root.transform, worldPositionStays: false);

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, worldPositionStays: false);
            var bg = bgGo.AddComponent<Image>();

            var view = go.AddComponent<T>();
            EditModeTestHelpers.Inject(view, "_background", bg);
            return (view, bg);
        }

        private static GameObject Ring(GameObject card) =>
            card.transform.Find("_SelectionRing")?.gameObject;

        // ── CardViewSpec contract (pure, no MonoBehaviour needed) ─────────────────

        [Test]
        public void CardViewSpec_IdleAndSelectedPanelsDiffer()
        {
            Assert.AreNotEqual(CardViewSpec.Idle, CardViewSpec.Selected,
                "Idle and Selected background colors must differ");
        }

        [Test]
        public void CardViewSpec_SelectedScaleIsAboveOne()
        {
            Assert.Greater(CardViewSpec.SelectedScale, 1f,
                "SelectedScale must be > 1 so the selected card visually lifts");
        }

        [Test]
        public void CardViewSpec_SelectedRingIsOpaque()
        {
            Assert.AreEqual(1f, CardViewSpec.SelectedRing.a, 1e-4f,
                "SelectedRing color must be fully opaque so the gold rim reads clearly");
        }

        [Test]
        public void CardViewSpec_IdleAndSelectedTextsDiffer()
        {
            Assert.AreNotEqual(CardViewSpec.IdleText, CardViewSpec.SelectedText,
                "IdleText and SelectedText must differ so selection changes label legibility");
        }

        // ── PathCardView ──────────────────────────────────────────────────────────

        [Test]
        public void PathCardView_SetSelected_True_BackgroundIsSelectedColor()
        {
            var view = MakePathCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);

            Assert.AreEqual(CardViewSpec.Selected, bg.color,
                "Selected PathCardView background must be CardViewSpec.Selected");
        }

        [Test]
        public void PathCardView_SetSelected_False_BackgroundIsIdleColor()
        {
            var view = MakePathCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(CardViewSpec.Idle, bg.color,
                "Deselected PathCardView background must revert to CardViewSpec.Idle");
        }

        [Test]
        public void PathCardView_SetSelected_True_ScaleIsSelectedScale()
        {
            var view = MakePathCard();

            view.SetSelected(true);

            float expected = CardViewSpec.SelectedScale;
            Assert.AreEqual(expected, view.transform.localScale.x, 1e-4f, "x scale when selected");
            Assert.AreEqual(expected, view.transform.localScale.y, 1e-4f, "y scale when selected");
        }

        [Test]
        public void PathCardView_SetSelected_False_ScaleIsOne()
        {
            var view = MakePathCard();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(1f, view.transform.localScale.x, 1e-4f, "x scale when deselected");
            Assert.AreEqual(1f, view.transform.localScale.y, 1e-4f, "y scale when deselected");
        }

        [Test]
        public void PathCardView_SetSelected_True_RingIsActive()
        {
            var view = MakePathCard();

            view.SetSelected(true);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring, "PathCardView must have a _SelectionRing child after Bind");
            Assert.IsTrue(ring.activeSelf, "Ring must be active when selected");
        }

        [Test]
        public void PathCardView_SetSelected_False_RingIsInactive()
        {
            var view = MakePathCard();

            view.SetSelected(true);
            view.SetSelected(false);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring, "PathCardView must have a _SelectionRing child after Bind");
            Assert.IsFalse(ring.activeSelf, "Ring must be inactive when deselected");
        }

        // ── OriCardView ───────────────────────────────────────────────────────────

        [Test]
        public void OriCardView_SetSelected_True_BackgroundIsSelectedColor()
        {
            var view = MakeOriCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);

            Assert.AreEqual(CardViewSpec.Selected, bg.color,
                "Selected OriCardView background must be CardViewSpec.Selected");
        }

        [Test]
        public void OriCardView_SetSelected_False_BackgroundIsIdleColor()
        {
            var view = MakeOriCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(CardViewSpec.Idle, bg.color,
                "Deselected OriCardView background must revert to CardViewSpec.Idle");
        }

        [Test]
        public void OriCardView_SetSelected_True_ScaleIsSelectedScale()
        {
            var view = MakeOriCard();

            view.SetSelected(true);

            float expected = CardViewSpec.SelectedScale;
            Assert.AreEqual(expected, view.transform.localScale.x, 1e-4f);
            Assert.AreEqual(expected, view.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void OriCardView_SetSelected_False_ScaleIsOne()
        {
            var view = MakeOriCard();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(1f, view.transform.localScale.x, 1e-4f);
            Assert.AreEqual(1f, view.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void OriCardView_SetSelected_True_RingIsActive()
        {
            var view = MakeOriCard();

            view.SetSelected(true);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring, "OriCardView must have a _SelectionRing child after Bind");
            Assert.IsTrue(ring.activeSelf, "Ring must be active when selected");
        }

        [Test]
        public void OriCardView_SetSelected_False_RingIsInactive()
        {
            var view = MakeOriCard();

            view.SetSelected(true);
            view.SetSelected(false);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring);
            Assert.IsFalse(ring.activeSelf, "Ring must be inactive when deselected");
        }

        // ── CrossroadsOptionView ──────────────────────────────────────────────────

        [Test]
        public void CrossroadsOptionView_SetSelected_True_BackgroundIsSelectedColor()
        {
            var view = MakeCrossroadsCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);

            Assert.AreEqual(CardViewSpec.Selected, bg.color,
                "Selected CrossroadsOptionView background must be CardViewSpec.Selected");
        }

        [Test]
        public void CrossroadsOptionView_SetSelected_False_BackgroundIsIdleColor()
        {
            var view = MakeCrossroadsCard();
            var bg   = view.transform.Find("Background").GetComponent<Image>();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(CardViewSpec.Idle, bg.color,
                "Deselected CrossroadsOptionView background must revert to CardViewSpec.Idle");
        }

        [Test]
        public void CrossroadsOptionView_SetSelected_True_ScaleIsSelectedScale()
        {
            var view = MakeCrossroadsCard();

            view.SetSelected(true);

            float expected = CardViewSpec.SelectedScale;
            Assert.AreEqual(expected, view.transform.localScale.x, 1e-4f);
            Assert.AreEqual(expected, view.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void CrossroadsOptionView_SetSelected_False_ScaleIsOne()
        {
            var view = MakeCrossroadsCard();

            view.SetSelected(true);
            view.SetSelected(false);

            Assert.AreEqual(1f, view.transform.localScale.x, 1e-4f);
            Assert.AreEqual(1f, view.transform.localScale.y, 1e-4f);
        }

        [Test]
        public void CrossroadsOptionView_SetSelected_True_RingIsActive()
        {
            var view = MakeCrossroadsCard();

            view.SetSelected(true);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring, "CrossroadsOptionView must have a _SelectionRing child after Bind");
            Assert.IsTrue(ring.activeSelf, "Ring must be active when selected");
        }

        [Test]
        public void CrossroadsOptionView_SetSelected_False_RingIsInactive()
        {
            var view = MakeCrossroadsCard();

            view.SetSelected(true);
            view.SetSelected(false);

            var ring = Ring(view.gameObject);
            Assert.IsNotNull(ring);
            Assert.IsFalse(ring.activeSelf, "Ring must be inactive when deselected");
        }
    }
}
