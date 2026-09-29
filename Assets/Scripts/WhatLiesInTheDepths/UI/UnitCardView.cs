// What Lies In The Depths — one Assault unit card. Spec section 9.
using System.Collections.Generic;
using System.Linq;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine.Economy;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class UnitCardView : MonoBehaviour
    {
        public Image ground;
        public Image border;
        public UiButton hover;
        public Image portrait;
        public TMP_Text unitName;
        public TMP_Text blurb;
        public Image powerGround;
        public Image powerBorder;
        public TMP_Text power;
        public Image ownedGround;
        public Image ownedBorder;
        public TMP_Text ownedPill;
        public RectTransform costs;
        public CostPill costPillPrefab;
        public UiButton muster;
        public Image musterGround;
        public TMP_Text musterLabel;
        public GameObject newDot;

        // The spec's raw values where it does not use a token.
        static Color PowerEdge => Theme.Mix(Tok.TealL, Tok.Teal, 0.22f);
        static Color DisabledInk => Theme.Mix(Tok.Ink3, Tok.Ink4, 0.55f);

        UnitDef _u;
        bool _hovered;
        readonly List<CostPill> _pills = new List<CostPill>();

        public void Bind(UnitDef u)
        {
            _u = u;
            Art.Apply(portrait, Art.Portrait(u.ArtKey));
            if (unitName != null) unitName.text = u.n;
            if (blurb != null) blurb.text = u.bl;
            if (musterLabel != null) musterLabel.text = Strings.T("ui.unit.muster");

            // "+12 strength, each": teal-l on #CFE5E1, the figure a step heavier.
            if (power != null) { power.text = Strings.T("ui.unit.power", "<b>+" + Fmt.Count(u.p) + "</b>"); power.color = Theme.Get(Tok.TealD); }
            if (powerGround != null) powerGround.color = Theme.Get(Tok.TealL);
            if (powerBorder != null) powerBorder.color = PowerEdge;

            // "×48": mono 11, iris-d, on veil at .92 with a haze edge.
            if (ownedGround != null) ownedGround.color = Theme.Get(Tok.Veil, 0.92f);
            if (ownedBorder != null) ownedBorder.color = Theme.Get(Tok.Haze);
            if (ownedPill != null) ownedPill.color = Theme.Get(Tok.IrisD);

            if (newDot != null) newDot.SetActive(u.isNew);
            if (hover != null)
                hover.Hovered += h =>
                {
                    _hovered = h;
                    // A new unit's dot clears the first time the pointer rests on it.
                    if (h && _u.isNew) { _u.isNew = false; if (newDot != null) newDot.SetActive(false); }
                    PaintCard();
                };

            if (muster != null)
                muster.Clicked += () =>
                {
                    GameState.I?.Dream.Muster(_u);
                };

            BuildPills();
            PaintCard();
            Refresh();
        }

        // Each one bought raises the next one's price by 1.15x.
        // Each one bought raises the price 15%; a won place can cut it, and the card's own
        // "-15%" is that cut written as the multiplier 0.85 rather than as the number 0.15.
        List<Amount> Escalated()
        {
            return GameState.I != null ? GameState.I.Dream.MusterCost(_u) : _u.cost;
        }

        void BuildPills()
        {
            _pills.Clear();
            if (costs == null || costPillPrefab == null) return;
            foreach (Transform c in costs) Destroy(c.gameObject);
            foreach (var a in _u.cost)
            {
                var pill = Instantiate(costPillPrefab, costs);
                Art.Apply(pill.glyph, Art.Resource(a.k));
                pill.ShowGlyph(true);
                ResourceHover.On(pill.gameObject, a.k);   // lights its row in the ledger
                _pills.Add(pill);
            }
        }

        void PaintCard()
        {
            // Hover rings and lightens the card.
            if (border != null) border.color = Theme.Get(_hovered ? Tok.IrisB : Tok.Haze);
            if (ground != null) ground.color = _hovered ? Theme.Get(Tok.Lit) : Theme.Get(Tok.Veil);
        }

        public void Refresh()
        {
            if (_u == null || GameState.I == null) return;
            var s = GameState.I;
            if (ownedPill != null) ownedPill.text = "×" + Fmt.Count(_u.c);
            // Strength moves with every effect on the army, so it is read every refresh.
            if (power != null) power.text = Strings.T("ui.unit.power", "<b>+" + Fmt.Count(_u.p) + "</b>");

            var cost = Escalated();
            for (int i = 0; i < _pills.Count && i < cost.Count; i++)
            {
                var a = cost[i];
                var res = s.Find(a.k);
                pillState(_pills[i], res != null ? res.n : a.k, a, s);
            }

            FitCosts();

            bool pay = s.Judge(cost) == Refusal.None;
            if (muster != null) muster.SetInteractable(pay);
            if (musterGround != null) musterGround.color = Theme.Get(pay ? Tok.Iris : Tok.Track);
            if (musterLabel != null) musterLabel.color = pay ? Theme.Get(Tok.OnIris) : DisabledInk;
        }

        // Beside Muster there is room for about 222 of pills. A late unit's three prices, with
        // four figures and names like Whispers, need half again that, and a squeezed row runs its
        // words into each other. So when the names will not all fit, every pill on the card drops
        // its name and keeps its glyph and amount (about 70 each at most) — all or none, so the
        // row never mixes the two. The names come back when the prices fall or the row widens.
        void FitCosts()
        {
            if (costs == null || _pills.Count == 0) return;
            var row = costs.GetComponent<HorizontalOrVerticalLayoutGroup>();
            float gap = row != null ? row.spacing : 5f;
            float room = costs.rect.width > 1f ? costs.rect.width : 222f;
            float need = gap * (_pills.Count - 1);
            foreach (var p in _pills) need += p.WidthWithLabel();
            bool names = need <= room + 0.5f;
            if (_pills[0].LabelShown == names) return;
            foreach (var p in _pills) p.ShowLabel(names);
        }

        // The Constructs cost pills unchanged, which includes both refusals: rose when you are
        // merely short, gold when the price is above the resource's ceiling and waiting will
        // never pay it. Judged ceiling-first, as everywhere else.
        static void pillState(CostPill pill, string name, Amount a, GameState s)
        {
            var state = s.AboveCeiling(a.k, a.n) ? Refusal.AboveCeiling
                      : s.Short(a.k, a.n) ? Refusal.Short
                      : Refusal.None;
            pill.Set(name, a.n, state);
        }
    }
}
