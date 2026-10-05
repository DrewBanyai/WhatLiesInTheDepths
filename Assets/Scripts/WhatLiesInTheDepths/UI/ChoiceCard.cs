// What Lies In The Depths — one side of the fork. RevelationsChoice spec, section 4.
//
// Not the single readout at half width. Two 600-wide readouts do not fit an 880 field, so the
// card is re-cut: the sigil panel that runs the full height of the readout becomes a 56px plate
// in the corner, and the foot stacks — pills, then a reason, then a button the whole width of
// the card. Two buttons of equal width facing each other across the rule is the picture of a
// choice; two buttons hugging their right edges is not.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine.Economy;

namespace WhatLiesInTheDepths.UI
{
    public sealed class ChoiceCard : MonoBehaviour
    {
        /// <summary>What a side of the fork can be: still on offer, the one taken, or the one
        /// given up. The last two only ever arrive together.</summary>
        public enum Mode { Offer, Taken, GivenUp }

        /// <summary>What the side given up falls to. It stays legible: the player has to be
        /// able to read the thing they did not take.</summary>
        public const float GoneAlpha = 0.34f;

        public CanvasGroup group;
        public Image plate;
        public Image plateBorder;
        public Image glyph;
        public TMP_Text kindCaption;
        public TMP_Text revelationName;
        public TMP_Text realization;
        public RectTransform effects;
        public RectTransform costs;
        public TMP_Text reasonLine;

        [Header("The foot's three faces, one at a time")]
        public GameObject realizeRoot;
        public UiButton realize;
        public Image realizeGround;
        public TMP_Text realizeLabel;
        public GameObject takenRoot;
        public TMP_Text takenLabel;
        public GameObject givenUpRoot;
        public TMP_Text givenUpLabel;

        public RevelationDef Def { get; private set; }
        public Mode Face { get; private set; } = Mode.Offer;

        readonly List<CostPill> _pills = new List<CostPill>();

        static Color DisabledInk => Theme.Mix(Tok.Ink3, Tok.Ink4, 0.55f);

        /// <summary>Fills the card from a Realization. Blue throughout: the plate, its edge, the
        /// kind and the button. Blue no longer carries the meaning on its own — the header and
        /// the word between the cards do that — but it is still what the pair is coloured.</summary>
        public void Bind(RevelationDef d, GameObject effectRowPrefab, CostPill pillPrefab)
        {
            Def = d;
            if (d == null) return;

            Art.Apply(glyph, Art.Sigil(string.IsNullOrEmpty(d.g) ? d.k : d.g));
            if (glyph != null) glyph.color = Theme.Get(Tok.BlueD);
            if (plate != null) plate.color = Theme.Get(Tok.BlueL);
            if (plateBorder != null) plateBorder.color = Theme.Get(Tok.BlueB);

            if (kindCaption != null)
            {
                kindCaption.text = (d.kind ?? string.Empty).ToUpperInvariant();
                kindCaption.color = Theme.Get(Tok.BlueD);
            }
            if (revelationName != null) revelationName.text = d.n;
            if (realization != null) realization.text = d.bl;

            if (effects != null && effectRowPrefab != null)
            {
                // Switched off before they go: Destroy waits for the end of the frame, and the
                // panel is measured before then.
                foreach (Transform c in effects) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                string teal = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(Tok.TealD));
                foreach (var f in d.fx)
                {
                    var go = Instantiate(effectRowPrefab, effects);
                    var t = go.GetComponentInChildren<TMP_Text>();
                    if (t == null) continue;
                    t.text = f.Replace("<b>", "<b><color=" + teal + ">").Replace("</b>", "</color></b>");
                    t.color = Theme.Get(Tok.Prose);
                    Ursine.Text.Typeset.Resize(t, 12f);
                }
            }

            if (costs != null && pillPrefab != null)
            {
                foreach (Transform c in costs) { c.gameObject.SetActive(false); Destroy(c.gameObject); }
                _pills.Clear();
                foreach (var a in d.cost)
                {
                    var pill = Instantiate(pillPrefab, costs);
                    pill.ShowGlyph(false);                    // the card names a cost in words
                    ResourceHover.On(pill.gameObject, a.k);   // lights its row in the ledger
                    _pills.Add(pill);
                }
            }
        }

        /// <summary>Everything that depends on the purse: each pill's refusal, the button and
        /// the reason line. Painted on opening and again whenever the ledger moves, so a cost
        /// that becomes payable while the panel is open turns payable in front of the player.
        ///
        /// Returns true when the reason line came, went or changed — the panel relays out then
        /// and not otherwise, because this runs on every tick.</summary>
        public bool PaintCost()
        {
            if (Def == null || GameState.I == null) return false;
            var state = GameState.I.Judge(Def.cost);
            Def.state = state;

            for (int i = 0; i < _pills.Count && i < Def.cost.Count; i++)
            {
                var a = Def.cost[i];
                var res = GameState.I.Find(a.k);
                Refusal pillState = GameState.I.AboveCeiling(a.k, a.n) ? Refusal.AboveCeiling
                                  : GameState.I.Short(a.k, a.n) ? Refusal.Short : Refusal.None;
                if (_pills[i] != null) _pills[i].Set(res != null ? res.n : a.k, a.n, pillState);
            }

            bool live = Face == Mode.Offer && state == Refusal.None;
            if (realize != null) realize.SetInteractable(live);
            if (realizeGround != null) realizeGround.color = Theme.Get(live ? Tok.BlueD : Tok.Track);
            if (realizeLabel != null)
            {
                realizeLabel.text = Strings.T("ui.revelations.realize");
                realizeLabel.color = live ? Theme.Get(Tok.OnIris) : DisabledInk;
            }

            // A card that cannot be paid does not fade. The choice has not been narrowed for
            // the player, only delayed, and a faded card says otherwise.
            bool moved = false;
            if (reasonLine != null)
            {
                string reason = Face == Mode.Offer ? (GameState.I.ReasonLine(Def.cost) ?? string.Empty)
                                                   : string.Empty;
                bool shown = reason.Length > 0;
                moved = shown != reasonLine.gameObject.activeSelf || reason != reasonLine.text;
                reasonLine.gameObject.SetActive(shown);
                reasonLine.text = reason;
            }
            return moved;
        }

        /// <summary>Which of the foot's three faces is showing. Nothing else about the card
        /// changes: the side given up keeps its words, its effects and its price.</summary>
        public void SetFace(Mode m)
        {
            Face = m;
            if (realizeRoot != null) realizeRoot.SetActive(m == Mode.Offer);
            if (takenRoot != null) takenRoot.SetActive(m == Mode.Taken);
            if (givenUpRoot != null) givenUpRoot.SetActive(m == Mode.GivenUp);

            if (takenLabel != null)
            {
                takenLabel.text = Strings.T("ui.choice.taken");
                takenLabel.color = Theme.Get(Tok.BlueD);
            }
            if (givenUpLabel != null)
            {
                givenUpLabel.text = Strings.T("ui.choice.givenUp").ToUpperInvariant();
                givenUpLabel.color = Theme.Get(Tok.Ink4);
            }
            PaintCost();
        }

        /// <summary>How far out the card is, 0 lit and 1 given up. Alpha alone: over the panel's
        /// own ground a card at a third strength loses its colour on its way out, which is the
        /// whole of what the spec asks for and costs no material.</summary>
        public void SetFade(float t)
        {
            if (group != null) group.alpha = Mathf.Lerp(1f, GoneAlpha, Mathf.Clamp01(t));
        }
    }
}
