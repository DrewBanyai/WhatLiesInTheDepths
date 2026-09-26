// What Lies In The Depths — one Construct card. Spec section 7.
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
    public sealed class ConstructCardView : MonoBehaviour
    {
        public UiButton hoverTarget;
        public Image cardGround;
        public Image cardBorder;
        public Image ring;
        public CanvasGroup cardGroup;
        public Image plate;
        public TMP_Text ownedCount;
        public TMP_Text constructName;
        public TMP_Text kind;
        public TMP_Text blurb;
        public RectTransform effects;
        public GameObject effectRowPrefab;
        public RectTransform costs;
        public CostPill costPillPrefab;
        public UiButton build;
        public Image buildGround;
        public TMP_Text buildLabel;
        public TMP_Text reasonLine;
        public AttentionDot dot;

        ConstructDef _c;
        bool _hovered;
        List<GameObject> _foot;

        /// <summary>The card's foot: the rule above it, the costs and the Build button. The card
        /// is laid out absolutely, so they are separate children rather than one row.</summary>
        List<GameObject> FootParts()
        {
            var parts = new List<GameObject>();
            if (costs != null) parts.Add(costs.gameObject);
            var rule = transform.Find("Rule");
            if (rule != null) parts.Add(rule.gameObject);
            if (build != null)
            {
                // The button's own root: its nearest ancestor that is a direct child of the card.
                var b = build.transform;
                while (b.parent != null && b.parent != transform) b = b.parent;
                parts.Add(b.gameObject);
            }
            return parts;
        }

        // The court card on the Mind Palace map is one card rebound on every hover. Its
        // handlers must be attached once, or a Build click after three hovers builds three.
        bool _wired;

        public void Bind(ConstructDef c)
        {
            _c = c;
            _hovered = false;
            // Its section's sky, with its own glyph composited on it.
            Art.Apply(plate, Art.ConstructPlate(c.ArtKey));
            if (constructName != null) constructName.text = c.n;
            if (kind != null)
                kind.text = (!string.IsNullOrEmpty(c.kindLabel) ? c.kindLabel
                            : Strings.T("construct.kind." + c.kind.ToString().ToLowerInvariant())).ToUpperInvariant();
            if (blurb != null) blurb.text = c.b;
            if (dot != null) dot.Set(!c.seen);

            if (effects != null && effectRowPrefab != null)
            {
                Rebuild.Clear(effects);
                // The Silent Altar has no effect, so it has no effects panel at all.
                bool anyFx = c.fx != null && c.fx.Count > 0;
                var panel = effects.parent != null && effects.parent != transform ? effects.parent.gameObject : effects.gameObject;
                if (panel.activeSelf != anyFx) panel.SetActive(anyFx);
                if (anyFx) foreach (var f in c.fx)
                {
                    var go = Instantiate(effectRowPrefab, effects);
                    var tx = go.GetComponentInChildren<TMP_Text>();
                    if (tx != null) tx.text = f;
                }
            }
            Fit(ExtraFor(c));

            if (!_wired)
            {
                _wired = true;
                if (hoverTarget != null)
                    hoverTarget.Hovered += h =>
                    {
                        _hovered = h;
                        if (h && !_c.seen) { _c.seen = true; dot?.Set(false); }
                        Paint(h);
                    };

                // The card is not a click target; the Build button is. One click, no confirm,
                // no quantity selector, no buy-ten. The price curve is the brake.
                if (build != null)
                    build.Clicked += () =>
                    {
                        if (GameState.I != null) GameState.I.Build(_c);
                    };
            }

            Refresh();
        }

        // ---- height ---------------------------------------------------------------

        /// <summary>The effects panel is drawn for two lines. Each line past that adds a row's
        /// height (17, plus the stack's 4 gap) to the panel and pushes the foot of the card down
        /// by the same, so a long list of effects never runs into the price and Build.</summary>
        const float RowStep = 21f;
        const int RowsDrawn = 2;

        public static float ExtraFor(ConstructDef c) => Mathf.Max(0, (c?.fx?.Count ?? 0) - RowsDrawn) * RowStep;

        static readonly string[] Foot = { "Rule", "Costs", "Build", "ReasonLine" };
        float _extra;
        bool _measured;
        float _rootHeight, _panelHeight, _rowsHeight;
        readonly List<RectTransform> _footRects = new List<RectTransform>();
        readonly List<float> _footY = new List<float>();

        /// <summary>Makes the card <paramref name="extra"/> taller than the prefab, the whole of
        /// it given to the effects panel. The list calls this with the tallest card's extra so
        /// every card in a grid keeps its foot on the same line.</summary>
        public void Fit(float extra)
        {
            var root = (RectTransform)transform;
            var rows = effects;
            var panel = rows != null ? rows.parent as RectTransform : null;
            if (!_measured)
            {
                _measured = true;
                _rootHeight = root.sizeDelta.y;
                if (panel != null) _panelHeight = panel.sizeDelta.y;
                if (rows != null) _rowsHeight = rows.sizeDelta.y;
                foreach (var name in Foot)
                {
                    var t = transform.Find(name) as RectTransform;
                    if (t == null) continue;
                    _footRects.Add(t);
                    _footY.Add(t.anchoredPosition.y);
                }
            }
            _extra = extra;
            root.sizeDelta = new Vector2(root.sizeDelta.x, _rootHeight + extra);
            if (panel != null && panel != root) panel.sizeDelta = new Vector2(panel.sizeDelta.x, _panelHeight + extra);
            if (rows != null) rows.sizeDelta = new Vector2(rows.sizeDelta.x, _rowsHeight + extra);
            for (int i = 0; i < _footRects.Count; i++)
                _footRects[i].anchoredPosition = new Vector2(_footRects[i].anchoredPosition.x, _footY[i] - extra);
        }

        /// <summary>The card's height as the prefab draws it, before any effects grow it.</summary>
        public float BaseHeight => _measured ? _rootHeight : ((RectTransform)transform).sizeDelta.y - _extra;

        public void Refresh()
        {
            if (_c == null || GameState.I == null) return;

            // Something built once and done — The Silent Altar — has nothing left to buy: no
            // count, no price and no Build, so it never looks like something to buy more of.
            bool finished = _c.once && _c.owned > 0;
            var state = finished ? Refusal.None : GameState.I.Judge(_c.cost);
            _c.state = state;
            if (_foot == null) _foot = FootParts();
            foreach (var part in _foot)
                if (part != null && part.activeSelf == finished) part.SetActive(!finished);

            if (ownedCount != null)
            {
                // The card appears reading x2, never x0 — the stock carries across an upgrade.
                ownedCount.text = "×" + Fmt.Count(_c.owned);
                // The pill behind the figure goes with it: a one-of-a-kind build (The Nightlight,
                // the Altar) and a card with none built yet show no count and no empty pill.
                bool counted = _c.owned > 0 && !_c.once;
                var pill = ownedCount.transform.parent;
                if (pill != null && pill.name == "OwnedPill") pill.gameObject.SetActive(counted);
                ownedCount.gameObject.SetActive(counted);
            }

            if (costs != null && costPillPrefab != null && !finished)
            {
                Rebuild.Clear(costs);
                foreach (var a in _c.cost)
                {
                    var pill = Instantiate(costPillPrefab, costs);
                    var res = GameState.I.Find(a.k);
                    Refusal ps = GameState.I.AboveCeiling(a.k, a.n) ? Refusal.AboveCeiling
                               : GameState.I.Short(a.k, a.n) ? Refusal.Short : Refusal.None;
                    pill.Set(res != null ? res.n : a.k, a.n, ps);
                    Art.Apply(pill.glyph, Art.Resource(a.k));
                }
            }

            // Short of a resource: only the failing pill goes rose; the card keeps full
            // opacity and its plate, because this resolves itself while the player watches.
            // Above a ceiling: the whole card drops to 50%, plate included.
            if (cardGroup != null) cardGroup.alpha = state == Refusal.AboveCeiling ? 0.5f : 1f;

            if (build != null) build.SetInteractable(state == Refusal.None);
            if (buildGround != null)
                buildGround.color = state == Refusal.None ? Theme.Get(Tok.Iris) : Theme.Get(Tok.Track);
            if (buildLabel != null)
            {
                buildLabel.text = Strings.T("ui.constructs.build");
                buildLabel.color = state == Refusal.None ? Theme.Get(Tok.Veil) : Theme.Get(Tok.Ink4);
            }

            if (reasonLine != null)
            {
                // When several costs fail the sentence names only the largest shortfall.
                string reason = state == Refusal.None ? null : GameState.I.ReasonLine(_c.cost);
                reasonLine.gameObject.SetActive(!string.IsNullOrEmpty(reason));
                reasonLine.text = reason ?? string.Empty;
            }

            // A refresh can arrive while the pointer is on the card — every second, or the
            // moment a Build spends — so the hover state is kept rather than painted away.
            Paint(_hovered);
        }

        void Paint(bool hovered)
        {
            // Hover rings and lightens the card, and there is no lift — the card is not
            // pressable, so it must not look it. Suppressed on both refusal states.
            bool allow = hovered && _c != null && _c.state == Refusal.None && !(_c.once && _c.owned > 0);
            if (cardBorder != null) cardBorder.color = Theme.Get(allow ? Tok.IrisB : Tok.Haze);
            if (cardGround != null) cardGround.color = allow ? Color.white : Theme.Get(Tok.Veil);
            if (ring != null) ring.color = Theme.Get(Tok.Iris, allow ? 0.10f : 0f);
        }
    }
}
