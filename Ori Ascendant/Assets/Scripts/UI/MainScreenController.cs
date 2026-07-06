using System;
using OriAscendant.Core;
using OriAscendant.Data;
using OriAscendant.Save;
using OriAscendant.Systems;
using OriAscendant.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OriAscendant.UI
{
    /// <summary>
    /// Interactive MainScreen surfaces (GAMEPLAY §3.2 zones 4–6): the Advance
    /// CTA, tap-to-channel on the portrait, and the one-time channel hint. Reads
    /// state every frame (cheap struct compares). Writes go through system APIs
    /// (TryAdvance / ChoosePath / ChannelTap) and, for the hint/coach gating,
    /// directly to the add-only channelHintShownAt / seenFlags fields on SaveData.
    /// Progress is conveyed by the vessel fill in MainScreenSkin (issue #28).
    /// </summary>
    public class MainScreenController : MonoBehaviour
    {
        [SerializeField] private GameplayConfig _config;
        [SerializeField] private TribulationConfig _tribulationConfig;
        [SerializeField] private Image _stormVignette;
        [SerializeField] private TribulationScreen _tribulationScreen;
        [SerializeField] private GameObject _ctaRoot;
        [SerializeField] private Button _advanceButton;
        [SerializeField] private TMP_Text _advanceLabel;
        [SerializeField] private Button _portraitButton;
        [SerializeField] private RectTransform _floatingTextAnchor;
        [SerializeField] private GameObject _hintRoot;
        [SerializeField] private PathScreenView _pathScreen;
        [SerializeField] private OriScreenView _oriScreen;
        [SerializeField] private Screens.CrossroadsScreenView _crossroadsScreen;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Screens.SettingsScreenView _settingsScreen;
        [SerializeField] private Button _ojaButton;
        [SerializeField] private Screens.OjaScreenView _ojaScreen;
        [SerializeField] private Screens.ContestScreenView _contestScreen;

        private static readonly Color ChannelColor = Palette.AseGold; // àṣẹ gold (D)

        private AseGenerationSystem _aseGeneration;
        private CultivationSystem _cultivation;
        private SaveManager _saveManager;
        private OriSystem _oriSystem;
        private Systems.CrossroadsSystem _crossroadsSystem;
        private Systems.MarketplaceSystem _marketplace;

        private float _secondsSinceLaunch;
        private string _lastCtaText;

        private const float HintAppearSeconds = 10f;

        // ---- How-to-play overlay (E) — a two-beat coach card, built in Start,
        //      no SerializeField needed. Beat 1 asks for a channel tap and is
        //      advanced by HandlePortraitTapped; Beat 2 teaches the loop + goal,
        //      then dismisses on card tap or after the dwell. Beat state is
        //      session-local by design — only the final dismissal persists
        //      (SeenFlags.HowToPlay, same write path as before). ----
        private GameObject _howToPlayRoot;
        private float _howToPlayAlpha; // fade-in/out; managed by TickHowToPlay
        private Image _howToPlayBg;    // full-card backdrop
        private TMP_Text _howToPlayCaption;
        private TMP_Text _howToPlayBody;
        private bool _howToPlaySecondBeat;    // false = "tap your cultivator" · true = loop + goal
        private float _howToPlayBeat2Elapsed; // dwell clock for the Beat 2 auto-dismiss
        private const float HowToPlayBeat2DwellSeconds = 8f;

        // Beat copy — placeholder wording; the §7.10 language pass finalizes it.
        private const string HowToPlayBeat1Line =
            "Tap your cultivator to draw Àṣẹ into the world.";
        private const string HowToPlayBeat2Line =
            "Àṣẹ fills you with light — reach a stage's peak, then Advance. " +
            "At your tier's peak, face the Crossing.";

        private void Awake()
        {
            if (_advanceButton != null) _advanceButton.onClick.AddListener(HandleAdvanceTapped);
            if (_portraitButton != null) _portraitButton.onClick.AddListener(HandlePortraitTapped);
            if (_settingsButton != null && _settingsScreen != null)
                _settingsButton.onClick.AddListener(_settingsScreen.Show);
            if (_ojaButton != null && _ojaScreen != null)
                _ojaButton.onClick.AddListener(_ojaScreen.Show);
            if (_hintRoot != null) _hintRoot.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_advanceButton != null) _advanceButton.onClick.RemoveListener(HandleAdvanceTapped);
            if (_portraitButton != null) _portraitButton.onClick.RemoveListener(HandlePortraitTapped);
            if (_settingsButton != null && _settingsScreen != null)
                _settingsButton.onClick.RemoveListener(_settingsScreen.Show);
            if (_ojaButton != null && _ojaScreen != null)
                _ojaButton.onClick.RemoveListener(_ojaScreen.Show);
        }

        private void Start()
        {
            _aseGeneration = ServiceLocator.Get<AseGenerationSystem>();
            _cultivation = ServiceLocator.Get<CultivationSystem>();
            ServiceLocator.TryGet(out _saveManager);
            ServiceLocator.TryGet(out _oriSystem);
            ServiceLocator.TryGet(out _crossroadsSystem);
            ServiceLocator.TryGet(out _marketplace);

            if (_ctaRoot != null) _ctaRoot.SetActive(true);

            BuildHowToPlayOverlay();
        }

        private void Update()
        {
            if (_cultivation == null || _aseGeneration == null) return;

            RefreshProgress();
            RefreshCta();
            TickHint();
            TickHowToPlay();
            TickOriPrompt();
            TickCrossroadsPrompt();
            TickContestPrompt();
        }

        // ---- post-Crossing re-vow: surface the modal on the first frame after
        //      Resolve clears chosenOri (cheap field compare; one-shot per gen).
        private void TickOriPrompt()
        {
            if (_oriScreen == null || _oriSystem == null) return;
            if (_oriSystem.HasChosen || _oriScreen.IsOpen) return;
            _oriScreen.Show();
        }

        // ---- pending crossroads: surface the modal once Àṣẹ passes the milestone
        //      and the crossroads fires. Patient — waits across sessions.
        private void TickCrossroadsPrompt()
        {
            if (_crossroadsScreen == null || _crossroadsSystem == null) return;
            if (!_crossroadsSystem.HasPending || _crossroadsScreen.IsOpen) return;
            _crossroadsScreen.Show();
        }

        // ---- pending contest: surface the modal once a rival House has been queued.
        //      Patient — waits across sessions. Does not auto-surface if the player
        //      has already declined (the House remains pending; the player opens Ọjà
        //      when ready, or it surfaces again next session).
        private void TickContestPrompt()
        {
            if (_contestScreen == null || _marketplace == null) return;
            if (!_marketplace.HasPending || _contestScreen.IsOpen) return;
            _contestScreen.Show();
        }

        // ---- storm vignette (driven from within-stage progress) ----

        private void RefreshProgress()
        {
            BigNumber target = _cultivation.CurrentTarget;
            if (target.IsZero) return;

            double ratio = (_aseGeneration.CurrentAse / target).ToDouble();
            RefreshStormVignette(ratio);
        }

        /// <summary>Ambient escalation stub (GAMEPLAY §3.5 buildup): vignette alpha
        /// steps at the config's ambient fractions while at the final stage. On a
        /// resume, this jumps straight to the current state — intermediate
        /// stingers never replay (§3.4 sequencing rule). Full art is Phase D.</summary>
        private void RefreshStormVignette(double ratio)
        {
            if (_stormVignette == null) return;

            float alpha = 0f;
            if (_cultivation.IsAtFinalStage && _tribulationConfig != null &&
                _tribulationConfig.ambientFractions != null)
            {
                var fractions = _tribulationConfig.ambientFractions;
                if (fractions.Length > 0 && ratio >= fractions[0]) alpha = 0.15f;
                if (fractions.Length > 1 && ratio >= fractions[1]) alpha = 0.35f;
            }

            var c = _stormVignette.color;
            if (!Mathf.Approximately(c.a, alpha))
            {
                c.a = alpha;
                _stormVignette.color = c;
            }
        }

        // ---- CTA (zone 6) ----

        private void RefreshCta()
        {
            string text;
            bool interactable;

            if (_cultivation.IsAtFinalStage)
            {
                // The CTA morph: armed the moment the 25M gate is met. (The
                // Welcome-Back modal sits above this canvas and blocks input, so
                // collect always resolves before the player can trigger this.)
                text = "Face the Tribulation";
                interactable = _cultivation.IsTribulationEligibleNow;
            }
            else
            {
                text = "Advance";
                interactable = _cultivation.CanAdvance();
            }

            if (_advanceButton != null && _advanceButton.interactable != interactable)
            {
                _advanceButton.interactable = interactable;
            }
            if (text != _lastCtaText)
            {
                _lastCtaText = text;
                if (_advanceLabel != null) _advanceLabel.text = text;
            }
        }

        private void HandleAdvanceTapped()
        {
            if (_cultivation.IsAtFinalStage)
            {
                if (_cultivation.IsTribulationEligibleNow && _tribulationScreen != null)
                {
                    _tribulationScreen.ShowConfirm();
                }
                return;
            }

            switch (_cultivation.TryAdvance())
            {
                case AdvanceOutcome.NeedsPathChoice:
                    if (_pathScreen != null) _pathScreen.Show();
                    break;
                case AdvanceOutcome.Advanced:
                    // Counter-flash rides OnStageAdvanced elsewhere; here just one
                    // quiet salute over the portrait, above the "+N" lane.
                    // FloatingText pools its instances and is RM-safe.
                    if (_floatingTextAnchor != null)
                        FloatingText.Spawn(_floatingTextAnchor, new Vector2(0f, 72f),
                            "Risen", Palette.AseGold); // copy: placeholder (§7.10)
                    break;
            }
        }

        // ---- tap-to-channel (zone 4, GAMEPLAY §5.3) ----

        private void HandlePortraitTapped()
        {
            var save = _saveManager?.Current;
            BigNumber before = _aseGeneration.CurrentAse;
            _aseGeneration.ChannelTap();
            BigNumber granted = _aseGeneration.CurrentAse - before;

            if (!granted.IsZero && _floatingTextAnchor != null)
            {
                FloatingText.Spawn(_floatingTextAnchor, new Vector2(0f, 40f), "+" + granted, ChannelColor);
            }

            // First real channel while the coach card shows: Beat 1's job is done.
            if (!granted.IsZero) AdvanceHowToPlayBeat();

            if (_hintRoot != null && _hintRoot.activeSelf) _hintRoot.SetActive(false);
            if (save != null && !save.HasSeen(SeenFlags.ChannelHint))
            {
                save.MarkSeen(SeenFlags.ChannelHint); // persists with the next save
            }
        }

        // ---- how-to-play overlay (E) — first-launch two-beat coach ----

        /// <summary>Builds the coach card in code on the main canvas: a lower-third
        /// panel that never covers the portrait, so the cultivator stays visible AND
        /// tappable while Beat 1 asks for the tap. Sits directly below the TitleScreen
        /// curtain (naturally hidden until the title is tapped away) but above the
        /// gameplay zones, so the card owns its own taps. No SerializeField, no scene wiring.</summary>
        private void BuildHowToPlayOverlay()
        {
            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            var save = _saveManager?.Current;
            // Build even if already seen — we need the root to set inactive; cheaper than
            // finding it later. TickHowToPlay hides it on the first frame when seen.

            var root = new GameObject("HowToPlayOverlay", typeof(RectTransform), typeof(CanvasRenderer));
            var rootRt = (RectTransform)root.transform;
            rootRt.SetParent(canvas.transform, false);
            // Just below the TitleScreen curtain (the last baked canvas child; its
            // controller GO stays active after dismissal, so it is a stable landmark)
            // and above every gameplay zone — the old low index (4) drew the card
            // UNDER the portrait/CTA, which muddled both visuals and raycasts.
            var titleScreen = UiBuilder.FindDeep(canvas.transform, "TitleScreen");
            if (titleScreen != null) rootRt.SetSiblingIndex(titleScreen.GetSiblingIndex());
            UiBuilder.Stretch(rootRt);
            _howToPlayRoot = root;

            // ponytail: no full-screen scrim — it would eat or dim the portrait taps
            // Beat 1 is asking for. The card's own backdrop is plenty. The root has
            // no Graphic, so everything outside the card passes raycasts through.

            // Card panel — lower third, clear of the portrait zone (~24–58% height
            // from top ⇒ anchors 0.42–0.76). Temporarily covers the idle CTA band,
            // which is disabled on a fresh save anyway.
            var card = new GameObject("HowToPlayCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var cardRt = (RectTransform)card.transform;
            cardRt.SetParent(rootRt, false);
            cardRt.anchorMin = new Vector2(0.08f, 0.10f);
            cardRt.anchorMax = new Vector2(0.92f, 0.40f);
            cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
            _howToPlayBg = card.GetComponent<Image>();
            _howToPlayBg.color = Palette.IndigoBase.WithAlpha(0.92f);
            _howToPlayBg.raycastTarget = true; // Beat 1: skip · Beat 2: dismiss

            // Hairline gold border.
            var border = new GameObject("HowToPlayBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var brt = (RectTransform)border.transform;
            brt.SetParent(cardRt, false);
            UiBuilder.Stretch(brt);
            var borderImg = border.GetComponent<Image>();
            borderImg.color = Palette.AseGold.WithAlpha(AseHeroSpec.HairlineBorderAlpha);
            borderImg.raycastTarget = false;

            // Caption — blank during Beat 1 (the tap target is the cultivator, not
            // this card); becomes "touch to continue" in Beat 2.
            _howToPlayCaption = AddLine(cardRt, "HTP_Prompt", "",
                new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f),
                TypographicScale.Caption, Palette.TextSecondary);

            // One beat-managed teaching line; AdvanceHowToPlayBeat swaps the copy.
            _howToPlayBody = AddLine(cardRt, "HTP_Body", HowToPlayBeat1Line,
                new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.78f),
                TypographicScale.BodySm, Palette.TextPrimary);

            // Card tap: skip ahead in Beat 1 (never dismisses the instructions on a
            // stray tap), dismiss in Beat 2. The real Beat 1 advance is a channel.
            var btn = card.AddComponent<Button>();
            btn.targetGraphic = _howToPlayBg;
            btn.onClick.AddListener(HandleHowToPlayCardTapped);

            // Initial visibility: hide immediately if already seen.
            bool shown = HowToPlayDecision.ShouldShow(save?.seenFlags ?? 0);
            root.SetActive(shown);
            _howToPlayAlpha = shown ? 0f : 1f; // fade in from 0 when shown
        }

        private static TMP_Text AddLine(
            RectTransform parent, string name,
            string text, Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TMP_Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Beat 1 → Beat 2. Fired by the first successful channel while the
        /// card shows (HandlePortraitTapped) or by a direct card tap (skip). The
        /// transition is alpha-only, reusing the TickHowToPlay ramp — instant under
        /// Reduce Motion.</summary>
        private void AdvanceHowToPlayBeat()
        {
            if (_howToPlaySecondBeat || _howToPlayRoot == null || !_howToPlayRoot.activeSelf) return;
            _howToPlaySecondBeat = true;
            _howToPlayBeat2Elapsed = 0f;
            if (_howToPlayBody != null) _howToPlayBody.text = HowToPlayBeat2Line;
            if (_howToPlayCaption != null) _howToPlayCaption.text = "touch to continue";
            _howToPlayAlpha = MotionHelper.IsReduceMotion() ? 1f : 0f; // re-fade the new copy in
        }

        private void HandleHowToPlayCardTapped()
        {
            // ponytail: a Beat 1 card tap skips ahead instead of dismissing — a stray
            // tap can never vanish the instructions, and the card can never strand.
            if (!_howToPlaySecondBeat) AdvanceHowToPlayBeat();
            else DismissHowToPlay();
        }

        private void DismissHowToPlay()
        {
            var save = _saveManager?.Current;
            if (save == null) return;
            save.MarkSeen(SeenFlags.HowToPlay);
            // Persist using the same save-write path as ChannelHint — the save manager is the sole writer.
            if (ServiceLocator.TryGet(out SaveManager sm)) sm.Save();
            if (_howToPlayRoot != null) _howToPlayRoot.SetActive(false);
        }

        private void TickHowToPlay()
        {
            if (_howToPlayRoot == null) return;
            var save = _saveManager?.Current;
            if (!HowToPlayDecision.ShouldShow(save?.seenFlags ?? 0))
            {
                if (_howToPlayRoot.activeSelf) _howToPlayRoot.SetActive(false);
                return;
            }
            // Fade in (or instant if Reduce Motion). Beat transitions reuse this
            // ramp — AdvanceHowToPlayBeat drops the alpha and this brings it home.
            bool rm = MotionHelper.IsReduceMotion();
            float dt = Time.unscaledDeltaTime;
            if (rm)
                _howToPlayAlpha = 1f;
            else
                _howToPlayAlpha = Mathf.MoveTowards(_howToPlayAlpha, 1f, dt * 2f); // 0.5s fade-in
            // Apply alpha to the card backing and beat texts directly (ponytail:
            // a direct nudge on three Graphics is simpler than a CanvasGroup).
            if (_howToPlayBg != null)
            {
                var c = _howToPlayBg.color;
                _howToPlayBg.color = new Color(c.r, c.g, c.b, 0.92f * _howToPlayAlpha);
            }
            if (_howToPlayBody != null)
                _howToPlayBody.color = _howToPlayBody.color.WithAlpha(_howToPlayAlpha);
            if (_howToPlayCaption != null)
                _howToPlayCaption.color = _howToPlayCaption.color.WithAlpha(_howToPlayAlpha);

            // Beat 2 dwell: once the loop line has been readable for a while, let it
            // go by itself — a player already channelling shouldn't hunt for a tap.
            if (_howToPlaySecondBeat)
            {
                _howToPlayBeat2Elapsed += dt;
                if (_howToPlayBeat2Elapsed >= HowToPlayBeat2DwellSeconds) DismissHowToPlay();
            }
        }

        // ---- one-time hint (GAMEPLAY §5.3 discoverability) ----

        private void TickHint()
        {
            var save = _saveManager?.Current;
            if (save == null || _hintRoot == null) return;

            // seenFlags.ChannelHint is the "never show again" authority — set when
            // the player actually channels (HandlePortraitTapped). Once set, nothing
            // below can re-show the hint.
            if (save.HasSeen(SeenFlags.ChannelHint))
            {
                if (_hintRoot.activeSelf) _hintRoot.SetActive(false);
                return;
            }

            // The coach card already teaches the same tap — keep the hint quiet and
            // hold the appear clock, so it counts from the card's dismissal instead.
            if (_howToPlayRoot != null && _howToPlayRoot.activeSelf)
            {
                if (_hintRoot.activeSelf) _hintRoot.SetActive(false);
                return;
            }

            _secondsSinceLaunch += Time.unscaledDeltaTime;

            // Write the appear timestamp once, after the appear delay.
            if (save.channelHintShownAt == 0 && _secondsSinceLaunch >= HintAppearSeconds)
                save.channelHintShownAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // ponytail: no auto-expiry — the old 6s window was easy to miss. Once
            // shown, the hint persists until the first channel marks it seen above.
            bool shouldShow = save.channelHintShownAt != 0;
            if (_hintRoot.activeSelf != shouldShow) _hintRoot.SetActive(shouldShow);
        }
    }
}
