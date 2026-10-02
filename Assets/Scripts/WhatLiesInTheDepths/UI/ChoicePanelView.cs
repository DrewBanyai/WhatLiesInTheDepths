// What Lies In The Depths — the fork's panel. RevelationsChoice spec.
//
// Two cards and the word between them. The pair used to be two sigils that happened to be blue,
// drifting a field apart on opposite rings, and the only statement of the rule was the last line
// of a nine-line list. Everything here exists to say the one thing that mattered and was never
// said: these are two halves of one decision, and taking either spends the other.
//
// Three places say it — the header over both cards, the word on the rule between them, and the
// line under the fork out in the field — and none of them is a colour.
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class ChoicePanelView : MonoBehaviour
    {
        /// <summary>The panel's one width, and its one position: 824 of the field's 880, centred,
        /// top 56. Too big to sit beneath the lantern where a read-back belongs, so the pane it
        /// reads back is behind it — the one pane in the menu that cannot stay in sight.</summary>
        public const float Width = 824f;

        /// <summary>The side given up goes out over this, and the panel then holds. The hold is
        /// the entire reason for the fork: it is the moment the game admits what the choice cost,
        /// and it is deliberately generous.</summary>
        const float FadeSeconds = 0.45f;
        const float HoldSeconds = 1.2f;

        [Header("The panel")]
        public CanvasGroup group;
        public UiButton hover;
        public RectTransform column;
        public Image ground;
        public Image border;
        public Image shadow;

        [Header("Header")]
        public TMP_Text headerCaption;
        public TMP_Text headerLine;
        public Image headerGround;
        public Image headerRule;

        [Header("The two cards, and the rule between them")]
        public ChoiceCard left;
        public ChoiceCard right;
        public Image divider;
        public TMP_Text word;
        public Image lozengeGround;

        [Header("Stamped into the cards")]
        public GameObject effectRowPrefab;
        public CostPill costPillPrefab;

        /// <summary>Raised once the beat has run its course and the panel is done being looked
        /// at. The menu closes it and rebuilds the field.</summary>
        public event System.Action Finished;

        /// <summary>True from the press until the hold is over. Nothing may close the panel or
        /// rebuild the field while it is: the player is watching something be spent.</summary>
        public bool Spending => _beat >= 0f;

        float _beat = -1f;
        bool _past;

        void Awake() { if (hover != null) hover.Hovered += h => Hovered?.Invoke(h); }

        public event System.Action<bool> Hovered;

        // ---- the two faces of the panel -----------------------------------------

        /// <summary>The fork, on offer. Both cards live, the header in the present tense.</summary>
        public void ShowOffer(RevelationDef a, RevelationDef b)
        {
            _beat = -1f;
            _past = false;
            Fill(a, b);
            left.SetFace(ChoiceCard.Mode.Offer);
            right.SetFace(ChoiceCard.Mode.Offer);
            left.SetFade(0f);
            right.SetFade(0f);
            Dress(false);
            Fit();
        }

        /// <summary>Read back from the lit pane, for the rest of the run: the side taken settled,
        /// the side given up still there and still legible. A record of what the player chose
        /// between, not only of what they chose. Only the header changes tense.</summary>
        public void ShowPast(RevelationDef taken, RevelationDef givenUp)
        {
            _beat = -1f;
            _past = true;
            Fill(taken, givenUp);
            left.SetFace(ChoiceCard.Mode.Taken);
            right.SetFace(ChoiceCard.Mode.GivenUp);
            left.SetFade(0f);
            right.SetFade(1f);
            Dress(true);
            Fit();
        }

        void Fill(RevelationDef a, RevelationDef b)
        {
            left.Bind(a, effectRowPrefab, costPillPrefab);
            right.Bind(b, effectRowPrefab, costPillPrefab);
            left.PaintCost();
            right.PaintCost();
        }

        /// <summary>The words, and the rule's colour. Everything else about the panel is the
        /// same whichever face it is wearing.</summary>
        void Dress(bool spent)
        {
            if (headerCaption != null)
            {
                headerCaption.text = Strings.T(spent ? "ui.choice.header.past" : "ui.choice.header")
                                            .ToUpperInvariant();
                headerCaption.color = Theme.Get(spent ? Tok.Ink3 : Tok.BlueD);
            }
            if (headerLine != null)
            {
                headerLine.text = Strings.T(spent ? "ui.choice.line.past" : "ui.choice.line");
                headerLine.color = Theme.Get(Tok.Ink2);
            }
            if (headerGround != null) headerGround.color = Theme.Get(spent ? Tok.Block : Tok.BlueL, spent ? 1f : 0.55f);
            if (headerRule != null) headerRule.color = Theme.Get(Tok.Haze2);

            if (ground != null) ground.color = Theme.Get(Tok.Veil);
            if (border != null) border.color = Theme.Get(spent ? Tok.Haze : Tok.BlueB);
            if (shadow != null) shadow.color = spent ? Theme.Get(Tok.Ink, 0.45f) : Theme.Get(Tok.BlueD, 0.40f);

            if (word != null)
            {
                word.text = Strings.T("ui.choice.or").ToUpperInvariant();
                word.color = Theme.Get(spent ? Tok.Ink4 : Tok.BlueD);
            }
            if (divider != null) divider.color = Theme.Get(spent ? Tok.Haze2 : Tok.Haze);
            if (lozengeGround != null) lozengeGround.color = Theme.Get(Tok.Veil);
        }

        // ---- the press, and the beat that shows the trade ------------------------

        /// <summary>One click, no confirmation, as everywhere else on the screen. The panel has
        /// already said what happens, twice, before the press.</summary>
        public void Press(ChoiceCard card)
        {
            if (Spending || _past || card == null || card.Def == null) return;
            var other = card == left ? right : left;
            if (GameState.I == null) return;

            // The beat is claimed BEFORE the dream is told. Realizing raises Changed inside
            // itself, and the menu rebuilds its field on that — which would take this panel
            // away on the same frame as the press, which is the whole thing being fixed.
            _beat = 0f;
            if (!GameState.I.Realize(card.Def)) { _beat = -1f; return; }

            card.SetFace(ChoiceCard.Mode.Taken);
            other.SetFace(ChoiceCard.Mode.GivenUp);
        }

        void Update()
        {
            if (_beat < 0f) return;
            _beat += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(_beat / FadeSeconds);
            var gone = left.Face == ChoiceCard.Mode.GivenUp ? left : right;
            gone.SetFade(t);
            // The word greys with the card it is beside, over the same span.
            if (word != null) word.color = Color.Lerp(Theme.Get(Tok.BlueD), Theme.Get(Tok.Ink4), t);
            if (divider != null) divider.color = Color.Lerp(Theme.Get(Tok.Haze), Theme.Get(Tok.Haze2), t);

            if (_beat < FadeSeconds + HoldSeconds) return;
            _beat = -1f;
            Finished?.Invoke();
        }

        // ---- the purse moves while it is open ------------------------------------

        public void PaintCosts()
        {
            if (_past || Spending) return;
            bool moved = left.PaintCost();
            moved |= right.PaintCost();
            if (moved) Fit();
        }

        /// <summary>The panel is as tall as the taller card. The cards are stretched to one
        /// height by the row that holds them and the shorter one's effects panel takes the slack,
        /// so the two feet sit on one line — a ragged pair of card bottoms reads as one of them
        /// being unfinished rather than as the other being larger.</summary>
        public void Fit()
        {
            if (column == null) return;
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
            float h = Mathf.Ceil(LayoutUtility.GetPreferredHeight(column));
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(Width, h);
            column.sizeDelta = new Vector2(column.sizeDelta.x, h);
            LayoutRebuilder.ForceRebuildLayoutImmediate(column);
        }
    }
}
