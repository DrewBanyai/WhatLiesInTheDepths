// What Lies In The Depths — a side column, when there is no room for a side column.
//
// It is not a second ledger or a second gauge. The whole column is lifted out of the row and
// parked in here, bar and panel and whatever else it carries, so nothing inside it is rebuilt,
// re-wired or told anything: the ledger keeps counting whether the drawer is open or shut,
// because it is the same ledger. When the row has room again the column is put back where it
// came from.
//
// Which column that is depends on the shape. Compact 1 folds the ledger away and the drawer
// comes in from the left; Compact 2 folds the gauge away and it comes in from the right. The
// mirror is the whole difference — one sign, carried through the sheet, the tab and the arrow.
//
// Two ways in. The handle on the system bar is the one that names itself, and the tab on the
// edge of the screen is the one that is where the column used to be. Because the tab rides out
// on the sheet's outer edge it is also the way back out, which means the thing you reached for
// to open it is still under your hand to shut it.
using WhatLiesInTheDepths.Core;
using UnityEngine;
using UnityEngine.UI;
using Ursine;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    [DisallowMultipleComponent]
    public sealed class ColumnDrawer : MonoBehaviour
    {
        public static ColumnDrawer I { get; private set; }

        [Tooltip("Covers the whole stage. Off entirely while both side columns are standing.")]
        public RectTransform host;
        public CanvasGroup hostGroup;

        [Tooltip("The dark over the rest of the screen; closes the drawer when pressed.")]
        public UiButton scrim;

        [Tooltip("The sheet that slides: a ground, and the column on it.")]
        public RectTransform sheet;

        [Tooltip("A hairline on whichever edge of the sheet faces the rest of the screen.")]
        public RectTransform sheetEdge;

        [Tooltip("Where the column is parked while it is in here.")]
        public RectTransform slot;

        [Header("The handle on the edge")]
        [Tooltip("Outside the host rather than in it, because the host fades with the drawer "
               + "and this has to be seen precisely when the drawer is not.")]
        public RectTransform tab;
        public UiButton tabButton;
        public Image tabArrow;
        [Tooltip("The three graphics the pulse breathes through. The hover wash is a separate "
               + "overlay, so it and the pulse never fight over the same color.")]
        public Image tabGround;
        public Image tabBorder;

        /// <summary>Opening is a control, so it moves at the speed every other control does.</summary>
        const float Seconds = Layout.MotionControl;

        /// <summary>One breath of the pulse. Slow on purpose: this is the one thing on the
        /// screen asking to be noticed out of the corner of an eye, and a fast blink reads as
        /// an error rather than an invitation.</summary>
        const float PulseSeconds = 1.9f;

        RectTransform _ledger, _gauge;
        Transform _ledgerHome, _gaugeHome;
        int _ledgerIndex = -1, _gaugeIndex = -1;

        Folded _fold = Folded.None;
        bool _right;                    // the drawer comes in from the right
        float _tabInset;                // how far the tab sits from its edge, as built
        float _sheetW;

        bool _open;                     // what was asked for
        float _t;                       // 0 shut, 1 open — not a bool, because it slides
        bool _pulsing;                  // so the resting colors are put back exactly once
        bool _sealed;                   // the ending is up, and nothing else is
        bool _ledgerArrived = true, _gaugeArrived = true;

        public bool IsOpen => _open;

        /// <summary>Which column is in here, if any. The handles ask this: in a shape where a
        /// panel is simply standing in the row, a control for it would be noise.</summary>
        public Folded Fold => _fold;

        public bool Holds(Folded what) => what != Folded.None && _fold == what;

        void Awake()
        {
            I = this;
            // Where the tab sits from its edge, as the builder placed it. Read once, here,
            // because everything that moves it afterwards measures from it.
            if (tab != null) _tabInset = Mathf.Abs(tab.anchoredPosition.x);
        }

        void OnDestroy() { if (I == this) I = null; }

        void Start()
        {
            if (scrim != null) scrim.Clicked += Shut;
            if (tabButton != null) tabButton.Clicked += Toggle;
            Paint(true);
            PaintTab();
        }

        // ---- which shape we are in -----------------------------------------------------

        /// <summary>Called by ScreenRoot after it has placed the center column and settled the
        /// bars. Owns both side columns outright: whichever is folded goes in here, and the
        /// other is put back in the row at the place the profile gives it.</summary>
        public void SetProfile(StageProfile p, RectTransform ledgerColumn, RectTransform gaugeColumn)
        {
            // The host starts inactive, so Awake may not have run yet — and until it has, the
            // static is null and anything asking which column is folded would be told "none",
            // which is exactly wrong. Claimed here too, where it is certainly early enough.
            I = this;

            Learn(ref _ledger, ref _ledgerHome, ref _ledgerIndex, ledgerColumn);
            Learn(ref _gauge, ref _gaugeHome, ref _gaugeIndex, gaugeColumn);

            _fold = p.Fold;
            _right = p.DrawerOnRight;
            _sheetW = Layout.SideColumnW + Layout.Margin * 2f;

            // Stand first, then park: a column being put back has to be out of the slot before
            // the other one is put into it, or they are both in there for an instant.
            if (p.LedgerStands) Stand(_ledger, _ledgerHome, _ledgerIndex, p.LedgerX, p);
            if (p.GaugeStands) Stand(_gauge, _gaugeHome, _gaugeIndex, p.GaugeX, p);

            if (p.Fold == Folded.None)
            {
                _open = false;
                _t = 0f;
                if (host != null) host.gameObject.SetActive(false);
            }
            else
            {
                Park(p.Fold == Folded.Ledger ? _ledger : _gauge, p);
            }

            PaintTab();
            // Standing in the row counts as being looked at, so changing shape can change the
            // answer even though nothing in the ledger did.
            LedgerAttention.Sync();
        }

        static void Learn(ref RectTransform held, ref Transform home, ref int index, RectTransform col)
        {
            if (col == null) return;
            held = col;
            if (home != null) return;
            home = col.parent;
            index = col.GetSiblingIndex();
        }

        /// <summary>Whether each panel has arrived at all. A handle for a panel the dream has
        /// not opened yet is a promise, so there is no handle until there is something behind it.</summary>
        public void SetArrived(bool ledger, bool gauge)
        {
            _ledgerArrived = ledger;
            _gaugeArrived = gauge;
            PaintTab();
        }

        /// <summary>The ending is up. It is drawn over everything and nothing else is reachable,
        /// so the drawer shuts and its handle goes with it.</summary>
        public void Seal(bool on)
        {
            _sealed = on;
            if (on) _open = false;
            Paint(true);
            PaintTab();
        }

        void Stand(RectTransform col, Transform home, int index, float x, StageProfile p)
        {
            if (col == null) return;
            if (home != null && col.parent != home)
            {
                col.SetParent(home, false);
                if (index >= 0) col.SetSiblingIndex(index);
            }
            col.anchoredPosition = new Vector2(x, -Layout.ColumnY);
            col.sizeDelta = new Vector2(Layout.SideColumnW, p.ColumnH);
            col.gameObject.SetActive(true);
        }

        void Park(RectTransform col, StageProfile p)
        {
            if (col == null) return;

            if (host != null)
            {
                host.sizeDelta = new Vector2(p.StageW, p.StageH);
                host.gameObject.SetActive(true);
            }

            // The sheet hangs off whichever edge it comes in from, and the slot keeps the
            // column 30 clear of both of the sheet's edges whichever way round that is.
            if (sheet != null)
            {
                var corner = new Vector2(_right ? 1f : 0f, 1f);
                sheet.anchorMin = sheet.anchorMax = corner;
                sheet.pivot = corner;
                sheet.sizeDelta = new Vector2(_sheetW, p.StageH);
            }
            if (sheetEdge != null)
            {
                // The hairline goes on the edge that faces the rest of the screen.
                sheetEdge.anchoredPosition = new Vector2(_right ? 0f : _sheetW - 1f, 0f);
                sheetEdge.sizeDelta = new Vector2(1f, p.StageH);
            }
            if (slot != null) slot.sizeDelta = new Vector2(Layout.SideColumnW, p.StageH);

            if (col.parent != slot)
            {
                col.SetParent(slot, false);
                _open = false;
                _t = 0f;
            }
            col.anchoredPosition = new Vector2(0f, -Layout.ColumnY);
            col.sizeDelta = new Vector2(Layout.SideColumnW, p.ColumnH);
            col.gameObject.SetActive(true);

            PlaceTab();
            Paint(true);
        }

        /// <summary>The tab hangs off the same edge the sheet does, so the two of them read as
        /// one thing with a handle on it rather than as a control that happens to be nearby.</summary>
        void PlaceTab()
        {
            if (tab == null) return;
            var edge = new Vector2(_right ? 1f : 0f, 0.5f);
            tab.anchorMin = tab.anchorMax = edge;
            tab.pivot = edge;
        }

        // ---- opening and shutting ------------------------------------------------------

        public void Toggle()
        {
            if (_fold == Folded.None || _sealed) return;
            _open = !_open;
            // Opening is seeing. Synced here rather than when the slide finishes, so the pulse
            // stops on the frame of the press instead of flashing on behind the sheet.
            LedgerAttention.Sync();
        }

        public void Shut()
        {
            _open = false;
            LedgerAttention.Sync();
        }

        void Update()
        {
            if (_fold == Folded.None) return;

            float target = _open ? 1f : 0f;
            if (!Mathf.Approximately(_t, target))
            {
                _t = Mathf.MoveTowards(_t, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, Seconds));
                Paint(false);
            }

            Pulse();
        }

        /// <summary>Shut, the sheet is its own width outside the screen and the whole host stops
        /// taking the pointer, so the rest of the stage behaves as though none of this is here.
        /// The tab is carried by exactly the same number, which is what keeps it on the sheet's
        /// edge the whole way across rather than jumping once it arrives.</summary>
        void Paint(bool snap)
        {
            if (snap) _t = _open ? 1f : 0f;
            float e = Mathf.SmoothStep(0f, 1f, _t);
            // Both hang off the same edge, but they are anchored to it and so measure from it
            // in opposite directions: for the sheet, away from the edge is off the screen; for
            // the tab, whose pivot is its outer side, away from the edge is into it.
            float away = _right ? 1f : -1f;
            float into = -away;

            if (sheet != null)
                sheet.anchoredPosition = new Vector2(Mathf.Lerp(away * _sheetW, 0f, e),
                                                     sheet.anchoredPosition.y);

            if (hostGroup != null)
            {
                hostGroup.alpha = e;
                hostGroup.blocksRaycasts = _t > 0.01f;
                hostGroup.interactable = hostGroup.blocksRaycasts;
            }

            if (tab != null)
                tab.anchoredPosition = new Vector2(into * (_tabInset + _sheetW * e),
                                                   tab.anchoredPosition.y);

            // The arrow says which way the sheet is about to go, not which way the tab is: out
            // of the screen while it is in, and back into it while it is out.
            if (tabArrow != null)
                tabArrow.rectTransform.localEulerAngles =
                    new Vector3(0f, 0f, _right ? Mathf.Lerp(180f, 0f, e) : Mathf.Lerp(0f, 180f, e));
        }

        /// <summary>While something in the ledger has not been looked at, the tab breathes iris.
        /// Only the ledger's: a resource appearing inside a shut drawer announces nothing, which
        /// is the whole reason for this, and the gauge has no equivalent — you do not miss the
        /// dive control, you go looking for it.
        ///
        /// The colors are taken from the palette every frame rather than cached, which is what
        /// lets the pulse survive a palette or contrast change without being told about it.</summary>
        void Pulse()
        {
            bool on = _fold == Folded.Ledger && !_open && !_sealed
                      && _ledgerArrived && LedgerAttention.Unseen;
            if (!on)
            {
                if (!_pulsing) return;
                _pulsing = false;
                Rest();
                return;
            }

            _pulsing = true;
            float p = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * (Mathf.PI * 2f / PulseSeconds));
            if (tabGround != null) tabGround.color = Color.Lerp(Theme.Get(Tok.Veil), Theme.Get(Tok.IrisL), p);
            if (tabBorder != null) tabBorder.color = Color.Lerp(Theme.Get(Tok.Haze), Theme.Get(Tok.Iris), p);
            if (tabArrow != null) tabArrow.color = Color.Lerp(Theme.Get(Tok.Ink3), Theme.Get(Tok.IrisD), p);
        }

        /// <summary>Back to the tokens the builder drew it with, which is also what a theme
        /// change would set them to, so the two can never disagree once the pulse has stopped.</summary>
        void Rest()
        {
            if (tabGround != null) tabGround.color = Theme.Get(Tok.Veil);
            if (tabBorder != null) tabBorder.color = Theme.Get(Tok.Haze);
            if (tabArrow != null) tabArrow.color = Theme.Get(Tok.Ink3);
        }

        void PaintTab()
        {
            if (tab == null) return;
            bool arrived = _fold == Folded.Ledger ? _ledgerArrived
                         : _fold == Folded.Gauge ? _gaugeArrived : false;
            bool on = _fold != Folded.None && arrived && !_sealed;
            if (tab.gameObject.activeSelf != on) tab.gameObject.SetActive(on);
        }
    }
}
