// What Lies In The Depths — center destination. Spec section 7.
// Two views of one menu. The Mind Palace is the default view; the list is behind a toggle.
// The toggle is a segmented pill, deliberately not the bar's underline, because this is a
// view of one destination and not another destination.
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
    public sealed class ConstructsView : MonoBehaviour, IStageFit
    {
        [Header("View row — the menu's name left, the toggle right")]
        public TMP_Text menuName;
        public SegmentedToggle viewToggle;

        [Header("Views")]
        public GameObject mapView;
        /// <summary>The map is the one surface in the center that is a drawing rather than
        /// a list: the palace grounds, the buildings on them and the court card are placed
        /// against each other, so a short stage scales the whole thing rather than cropping
        /// it or reflowing it. Its own type comes down with it, which is the price — and it
        /// is still larger than it was, because everything around it grew by more.</summary>
        const float MapH = 860f, MapY = 36f;
        public GameObject listView;

        [Header("List")]
        public RectTransform listContent;
        public GameObject sectionHeadPrefab;
        public ConstructCardView cardPrefab;

        [Header("Map")]
        public RectTransform mapRoot;
        public GameObject buildingPrefab;
        public RectTransform courtCard;
        public ConstructCardView courtCardView;
        public CanvasGroup groundsGroup;

        [Header("Pointer — the map's empty ground, and the card's close control")]
        public UiButton mapHover;
        public UiButton cardHover;
        public UiButton cardClose;
        public Image cardCloseBorder;
        public Image cardCloseGlyph;

        // Resting on a building only lights it. Clicking it opens its card in the court, and
        // the card stays until it is closed: by its X, by clicking the same building again,
        // or by clicking the map's empty ground. Clicking another building swaps the card.
        // Hovering used to open the card, and reaching for it could brush a neighbour on the
        // way and swap it out from under the pointer.
        sealed class Building
        {
            public ConstructDef def;
            public CanvasGroup group;
            public TMP_Text badge;
            public GameObject badgePill;
            public Image form;
            public GameObject ring;
            public GameObject shadow;
            public GameObject root;
            /// <summary>The Silent Altar: the altar alone, ghosted above the stone until built.</summary>
            public Image top;
            /// <summary>The Silent Altar's one line, under it on hover. It has no card.</summary>
            public TMP_Text line;
            /// <summary>The halo under the form, lit while the pointer rests on it: iris when one
            /// more can be bought, white when it cannot — so it says whether a click is worth it.</summary>
            public GameObject halo;
            public Image haloFill;
            public Image haloEdge;
            /// <summary>The circle an unbuilt plot lights with, filling its dashed ring.</summary>
            public GameObject haloRound;
            public Image haloRoundFill;
            public Image haloRoundEdge;
            public bool hovered;
        }

        string _built;

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var c in s.ShownConstructs) b.Append(c.g).Append(',');
            b.Append('|').Append(s.Version);
            return b.ToString();
        }

        readonly List<Building> _buildings = new List<Building>();
        ConstructDef _open;

        readonly List<ConstructCardView> _cards = new List<ConstructCardView>();

        /// <summary>Scaled to whatever height the track has left under the view row, and kept
        /// centered across the column so the margin it gives up is split either side rather than
        /// all falling to the right. Never scaled up: 1 is how the map is drawn.</summary>
        public void Fit(StageProfile profile)
        {
            var rt = mapView != null ? mapView.transform as RectTransform : null;
            if (rt == null) return;

            float room = profile.TrackH - MapY;
            float k = Mathf.Clamp(room / MapH, 0.5f, 1f);
            rt.localScale = new Vector3(k, k, 1f);
            rt.anchoredPosition = new Vector2(Layout.CenterColumnW * (1f - k) * 0.5f, -MapY);
        }

        void Start()
        {
            if (menuName != null) menuName.text = Strings.T("ui.constructs.menu");

            BuildList();
            BuildMap();
            _built = Signature();

            if (viewToggle != null)
            {
                viewToggle.SetSilently(0);      // the Mind Palace is the default view
                viewToggle.Selected += i =>
                {
                    if (mapView != null) mapView.SetActive(i == 0);
                    if (listView != null) listView.SetActive(i == 1);
                };
            }
            if (mapView != null) mapView.SetActive(true);
            if (listView != null) listView.SetActive(false);
            if (mapHover != null) mapHover.Clicked += Close;          // the empty ground
            if (cardClose != null)
            {
                cardClose.Clicked += Close;
                cardClose.Hovered += h =>
                {
                    if (cardCloseBorder != null) cardCloseBorder.color = Theme.Get(h ? Tok.IrisB : Tok.Haze);
                    if (cardCloseGlyph != null) cardCloseGlyph.color = Theme.Get(h ? Tok.IrisD : Tok.Ink3);
                };
            }
            Close();

            if (GameState.I != null) GameState.I.Changed += RefreshAll;
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= RefreshAll;
        }

        void BuildList()
        {
            var s = GameState.I;
            if (s == null || listContent == null || cardPrefab == null) return;

            Rebuild.Clear(listContent);
            _cards.Clear();

            // The Silent Altar always comes first: above every section, alone on its row,
            // because it belongs to no section.
            var landmarks = s.ShownConstructs.Where(c => c.landmark).ToList();
            if (landmarks.Count > 0)
            {
                var lg = new GameObject("Landmark", typeof(RectTransform), typeof(GridLayoutGroup));
                var lrt = (RectTransform)lg.transform;
                lrt.SetParent(listContent, false);
                var grid1 = lg.GetComponent<GridLayoutGroup>();
                float extra1 = landmarks.Max(c => ConstructCardView.ExtraFor(c));
                var size1 = PrefabRect.Size(cardPrefab);
                grid1.cellSize = new Vector2(size1.x, size1.y + extra1);
                grid1.spacing = new Vector2(16f, 16f);
                grid1.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid1.constraintCount = 1;
                foreach (var c in landmarks)
                {
                    var card = Instantiate(cardPrefab, lrt);
                    card.Bind(c);
                    card.Fit(extra1);
                    _cards.Add(card);
                }
            }

            // Sections never collapse and never hide. The whole menu is one scrolling column.
            foreach (ConstructKind kind in System.Enum.GetValues(typeof(ConstructKind)))
            {
                var inSection = s.ShownConstructs.Where(c => c.kind == kind && !c.landmark).ToList();
                if (inSection.Count == 0) continue;      // locked constructs are absent entirely

                if (sectionHeadPrefab != null)
                {
                    var head = Instantiate(sectionHeadPrefab, listContent);
                    var t = head.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = Strings.T("construct.kind." + kind.ToString().ToLowerInvariant()).ToUpperInvariant();
                }

                var grid = new GameObject(kind + " grid", typeof(RectTransform), typeof(GridLayoutGroup));
                var grt = (RectTransform)grid.transform;
                grt.SetParent(listContent, false);
                var g = grid.GetComponent<GridLayoutGroup>();
                // As in Focus: the cell is the card, not a number that has to match it — grown
                // by the section's longest list of effects, so every foot sits on one line.
                float extra = inSection.Max(c => ConstructCardView.ExtraFor(c));
                var size = PrefabRect.Size(cardPrefab);
                g.cellSize = new Vector2(size.x, size.y + extra);
                g.spacing = new Vector2(16f, 16f);
                g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                g.constraintCount = 2;

                foreach (var c in inSection)
                {
                    var card = Instantiate(cardPrefab, grt);
                    card.Bind(c);
                    card.Fit(extra);
                    _cards.Add(card);
                }
            }
        }

        void BuildMap()
        {
            var s = GameState.I;
            if (s == null || mapRoot == null || buildingPrefab == null) return;

            foreach (var old in _buildings) if (old.root != null) { old.root.SetActive(false); Destroy(old.root); }
            _buildings.Clear();

            // Locked constructs are absent entirely.
            foreach (var c in s.ShownConstructs)
            {
                var go = Instantiate(buildingPrefab, mapRoot);
                var rt = (RectTransform)go.transform;
                // The spec's .bld carries margin -50 / -50 on a 100 x 100 box, so a building's
                // coordinates are its center, not its top-left corner. Placed as a corner they
                // all sat half a building down and to the right, which put the southern ones
                // over the map's own edge.
                rt.anchoredPosition = new Vector2(c.palacePos.x - rt.rect.width * 0.5f,
                                                  -(c.palacePos.y - rt.rect.height * 0.5f));

                var badge = go.GetComponentInChildren<TMP_Text>();

                // Look the parts up by name. GetComponentInChildren<Image>() returns the first
                // image in the hierarchy, which is the shadow, not the form.
                var form = go.transform.Find("Form")?.GetComponent<Image>();
                Art.Apply(form, Art.MapForm(c.ArtKey));
                var ring = go.transform.Find("UnbuiltRing");

                // The hit area is a child, so GetComponent on the root never found it and the
                // map never answered the pointer.
                var btn = go.GetComponentInChildren<UiButton>();
                var captured = c;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null) group = go.AddComponent<CanvasGroup>();
                var building = new Building
                {
                    def = c,
                    group = group,
                    badge = badge,
                    // The count's pill, not the figure inside it: hiding only the figure left
                    // an empty white lozenge on every plot, and on the north-east one it sat
                    // over the word DWELLINGS.
                    badgePill = badge != null && badge.transform.parent != null
                                ? badge.transform.parent.gameObject : null,
                    form = form,
                    ring = ring != null ? ring.gameObject : null,
                    shadow = go.transform.Find("Shadow")?.gameObject,
                    halo = go.transform.Find("Halo")?.gameObject,
                    haloFill = go.transform.Find("Halo")?.GetComponent<Image>(),
                    haloEdge = go.transform.Find("Halo/Edge")?.GetComponent<Image>(),
                    haloRound = go.transform.Find("HaloRound")?.gameObject,
                    haloRoundFill = go.transform.Find("HaloRound")?.GetComponent<Image>(),
                    haloRoundEdge = go.transform.Find("HaloRound/Edge")?.GetComponent<Image>(),
                    root = go
                };
                if (c.landmark) MakeLandmark(building, form);
                _buildings.Add(building);
                PaintBuilding(building);
                if (c.landmark)
                {
                    // No card: resting on it shows one line under it, and only while no card is
                    // open. The one building bought by clicking the building itself, because it
                    // is the only one with no card to carry a Build button.
                    var b = building;
                    if (btn != null)
                    {
                        btn.Hovered += h => { b.hovered = h; PaintBuilding(b); Fade(); };
                        btn.Clicked += () =>
                        {
                            // With a card open, the court is the card's: a click there closes it.
                            if (_open != null) { Close(); return; }
                            if (b.def.built) return;
                            if (GameState.I != null) GameState.I.Build(b.def);
                        };
                    }
                }
                else if (btn != null)
                {
                    var b = building;
                    btn.Hovered += h => { b.hovered = h; PaintBuilding(b); Fade(); };
                    btn.Clicked += () => { if (_open == captured) Close(); else Open(captured); };
                }
            }
        }

        /// <summary>The Silent Altar's extra parts: the altar drawn separately so the stone can
        /// stand at full ink beneath its ghost, and the line it shows on hover.</summary>
        void MakeLandmark(Building b, Image form)
        {
            if (b.badgePill != null) b.badgePill.SetActive(false);
            if (form != null)
            {
                var topGo = Instantiate(form.gameObject, form.transform.parent);
                topGo.name = "FormTop";
                topGo.transform.SetSiblingIndex(form.transform.GetSiblingIndex() + 1);
                b.top = topGo.GetComponent<Image>();
                b.top.raycastTarget = false;
            }

            var lineGo = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rt = (RectTransform)lineGo.transform;
            rt.SetParent(b.root.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            // Centered 94 below the altar's center, in the court's empty ground.
            rt.anchoredPosition = new Vector2(50f, -(50f + 94f - 10f));
            rt.sizeDelta = new Vector2(320f, 20f);
            var text = lineGo.GetComponent<TextMeshProUGUI>();
            Ursine.Text.Typeset.Set(text, Ursine.Text.TypeRole.SerifItalic, 13.5f, (int)Tok.Ink2);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            b.line = text;
            lineGo.SetActive(false);
        }

        // The card appears in the court, the grounds drop to .3 and the other buildings to .2.
        void Open(ConstructDef c)
        {
            _open = c;
            if (courtCard != null) courtCard.gameObject.SetActive(true);
            if (courtCardView != null) courtCardView.Bind(c);
            if (groundsGroup != null) groundsGroup.alpha = 0.3f;
            foreach (var b in _buildings) PaintBuilding(b);
            Fade();
        }

        void Close()
        {
            _open = null;
            if (courtCard != null) courtCard.gameObject.SetActive(false);
            if (groundsGroup != null) groundsGroup.alpha = 1f;
            foreach (var b in _buildings) PaintBuilding(b);
            Fade();
        }

        /// <summary>With no card open every building stands at full ink. With one open, its
        /// own building stays at full ink and the rest drop to .2 — except the one the pointer
        /// rests on, which comes up to .6 so a swap can be seen before it is clicked.</summary>
        void Fade()
        {
            foreach (var b in _buildings)
            {
                if (b.group == null) continue;
                b.group.alpha = _open == null || b.def == _open ? 1f : b.hovered ? 0.6f : 0.2f;
            }
        }

        /// <summary>Everything the purse can change: the list's cards, the open court card —
        /// a Build that leaves you short of the next one has to gray its own button while the
        /// pointer is still on it — and the map, where a first Build raises a plot.</summary>
        void RefreshAll()
        {
            if (Signature() != _built)
            {
                Close();
                BuildList();
                BuildMap();
                _built = Signature();
                return;
            }
            foreach (var c in _cards) c.Refresh();
            if (_open != null && courtCardView != null) courtCardView.Refresh();
            foreach (var b in _buildings) PaintBuilding(b);
        }

        // An unbuilt plot is a dashed ring with the map form at .22 and no badge.
        // Locked constructs are absent entirely, as everywhere. A one-of-a-kind build (The
        // Nightlight, The Waking Forge, The Beacon, The Night Kiln) has no badge built either:
        // there is only ever one, as with the Altar.
        void PaintBuilding(Building b)
        {
            if (b?.def == null) return;
            {
                // Lit under the pointer, and kept lit on the building whose card is open. A
                // plot not yet built lights as a circle filling its dashed ring; a standing
                // building as the rounded square.
                bool lit = b.hovered || (_open != null && b.def == _open);
                bool plot = !b.def.built && b.haloRound != null;
                bool square = lit && !plot, round = lit && plot;
                if (b.halo != null && b.halo.activeSelf != square) b.halo.SetActive(square);
                if (b.haloRound != null && b.haloRound.activeSelf != round) b.haloRound.SetActive(round);
                if (lit) PaintHalo(b);
            }
            if (b.def.landmark) { PaintLandmark(b); return; }
            bool counted = b.def.built && b.def.owned > 0 && !b.def.once;
            if (b.badge != null) b.badge.text = Fmt.Count(b.def.owned);
            var pill = b.badgePill != null ? b.badgePill : b.badge != null ? b.badge.gameObject : null;
            if (pill != null && pill.activeSelf != counted) pill.SetActive(counted);
            if (b.form != null) b.form.color = new Color(1f, 1f, 1f, b.def.built ? 1f : 0.22f);
            if (b.ring != null) b.ring.SetActive(!b.def.built);
            // Spec: a plot casts no shadow — there is nothing standing there yet.
            if (b.shadow != null) b.shadow.SetActive(b.def.built);
        }

        /// <summary>Iris, light, when the purse covers one more; white with a haze edge when it
        /// does not, or when there is nothing left to buy (a one-of-a-kind already standing);
        /// gold, the color of an unreachable figure, when some part of the price is above that
        /// resource's ceiling, so no amount of waiting will buy it.</summary>
        static void PaintHalo(Building b)
        {
            Color A(Tok t, float a) { var c = Theme.Get(t); c.a = a; return c; }
            var s = GameState.I;
            bool finished = b.def.once && b.def.owned > 0;
            bool can = !finished && s != null && s.Judge(b.def.cost) == Refusal.None;
            bool beyond = !finished && !can && s != null && b.def.cost != null
                          && b.def.cost.Any(a => s.AboveCeiling(a.k, a.n));
            Color fill = can ? A(Tok.IrisL, 0.6f) : beyond ? A(Tok.GoldL, 0.85f) : A(Tok.Lit, 0.8f);
            Color edge = can ? A(Tok.IrisB, 0.55f) : beyond ? A(Tok.GoldB, 0.95f) : A(Tok.Haze, 0.9f);
            if (b.haloFill != null) b.haloFill.color = fill;
            if (b.haloEdge != null) b.haloEdge.color = edge;
            if (b.haloRoundFill != null) b.haloRoundFill.color = fill;
            if (b.haloRoundEdge != null) b.haloRoundEdge.color = edge;
        }

        /// <summary>The stone is always there at full ink — it is where the dreamer sits.
        /// Unbuilt, only the altar above it is ghosted, inside the dashed ring, and its line is
        /// its price. Built, it stands whole, with no badge: there is only ever one.</summary>
        void PaintLandmark(Building b)
        {
            bool built = b.def.built && b.def.owned > 0;
            if (b.badgePill != null && b.badgePill.activeSelf) b.badgePill.SetActive(false);
            if (b.form != null)
            {
                Art.Apply(b.form, Art.MapForm(built ? b.def.ArtKey : b.def.ArtKey + "_stone"));
                b.form.color = Color.white;
            }
            if (b.top != null)
            {
                b.top.gameObject.SetActive(!built);
                Art.Apply(b.top, Art.MapForm(b.def.ArtKey + "_top"));
                b.top.color = new Color(1f, 1f, 1f, 0.22f);
            }
            if (b.ring != null) b.ring.SetActive(!built);
            if (b.shadow != null) b.shadow.SetActive(built);

            if (b.line != null)
            {
                bool show = b.hovered && _open == null;
                if (b.line.gameObject.activeSelf != show) b.line.gameObject.SetActive(show);
                if (show)
                {
                    if (built) b.line.text = b.def.line;
                    else
                    {
                        var s = GameState.I;
                        var cost = b.def.cost != null && b.def.cost.Count > 0 ? b.def.cost[0] : null;
                        bool isShort = s != null && s.Judge(b.def.cost) != Refusal.None;
                        string figure = cost != null ? Fmt.Amount(cost.n) : "";
                        string name = cost != null ? (s?.Find(cost.k)?.n ?? cost.k) : "";
                        string ink = "#" + ColorUtility.ToHtmlStringRGB(Theme.Get(isShort ? Tok.RoseD : Tok.Ink));
                        b.line.text = $"<i><color={ink}>{figure}</color></i> {name}";
                    }
                }
            }
        }
    }
}
