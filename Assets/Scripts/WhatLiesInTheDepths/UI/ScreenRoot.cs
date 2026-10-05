// What Lies In The Depths — the stage. Three columns absolutely positioned, top-aligned,
// never stretched: the space between them is margin, not flex. Extra width goes to the outer
// margins and the columns stay put (spec section 1).
//
// The stage comes in two sizes — see StageProfile for why and for the arithmetic. Everything
// below that moves when the size changes moves HERE, in one place, so that no panel has to
// know which shape it is in; the handful that cannot simply be made shorter say so by
// implementing IStageFit and are told.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace WhatLiesInTheDepths.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenRoot : MonoBehaviour
    {
        [Header("The stage")]
        [Tooltip("The 1920 x 1080 node the columns sit on; resized to the profile.")]
        public RectTransform stage;
        [Tooltip("The center column's track, which every destination and page is sized to.")]
        public RectTransform surfaces;

        [Header("Columns")]
        [Tooltip("Where a side column goes when the row has no room for it.")]
        public ColumnDrawer drawer;
        public RectTransform leftColumn;
        public RectTransform centerColumn;
        public RectTransform rightColumn;
        public CanvasGroup columnsGroup;

        [Header("Bars")]
        public BarView leftBar;
        public BarView centerBar;
        public BarView rightBar;

        [Header("Center surfaces, in Destination order")]
        public List<GameObject> destinations = new List<GameObject>();

        [Header("Center utility surfaces")]
        public GameObject optionsPage;
        public GameObject exitQuestion;
        public GameObject hardResetQuestion;
        public GameObject achievementsPage;

        [Header("The ending — not a column, and not an overlay")]
        public GameObject ending;
        public CanvasGroup endingGroup;

        Router _router;
        GameObject _ledgerPanel, _gaugePanel;
        Coroutine _endingFade;
        StageProfile _profile;

        // ---- the two shapes ----------------------------------------------------------

        /// <summary>Puts the columns where the profile says they go and makes every center
        /// surface as tall as its track. Called by the director on start and whenever the
        /// window crosses the breakpoint; safe to call with the profile already in effect.</summary>
        public void ApplyProfile(StageProfile p)
        {
            if (p == null) return;
            _profile = p;

            if (stage != null) stage.sizeDelta = new Vector2(p.StageW, p.StageH);

            // The center is the only column this places. The two side columns belong to the
            // drawer in every shape — one of them may be in it — so there is one owner for
            // where they are rather than two that have to agree.
            Place(centerColumn, p.CenterX, p.ColumnH);
            Bars(p);
            if (drawer != null) drawer.SetProfile(p, leftColumn, rightColumn);

            if (surfaces != null)
                surfaces.sizeDelta = new Vector2(Layout.CenterColumnW, p.TrackH);
            for (int i = 0; i < destinations.Count; i++) Track(destinations[i], p);
            Track(optionsPage, p);
            Track(exitQuestion, p);
            Track(hardResetQuestion, p);
            Track(achievementsPage, p);

            // The ending is not in a column: it is the whole stage, so it takes the stage.
            if (ending != null)
            {
                var rt = ending.transform as RectTransform;
                if (rt != null) rt.sizeDelta = new Vector2(p.StageW, p.StageH);
            }

            // Inactive ones too: a destination that is told its track only when it is opened
            // would be laid out at the old height for the frame in which it opens.
            foreach (var fit in GetComponentsInChildren<IStageFit>(true)) fit.Fit(p);
        }

        /// <summary>Which bar sits over which side column. The standing one carries the
        /// system bar, because Achievements, Options and Exit have to be reachable and a folded
        /// column is not; the folded one gets the community bar, which can wait behind a drawer.
        /// In Full and Compact 1 that is already where they are, so only Compact 2 moves them.
        ///
        /// The system bar pins to whichever screen edge its column stands against — the far
        /// corner is the point of it, and in Compact 2 the far corner is the other one.</summary>
        void Bars(StageProfile p)
        {
            var social = p.SwapBars ? rightColumn : leftColumn;
            var system = p.SwapBars ? leftColumn : rightColumn;
            Rehome(leftBar, social);
            Rehome(rightBar, system);
            if (rightBar != null)
                rightBar.Align(p.SwapBars ? TextAnchor.LowerLeft : TextAnchor.LowerRight);
        }

        /// <summary>Both side columns are 450 wide in every shape, so a bar moving between them
        /// keeps the size and the local place the builder gave it and needs nothing re-laid.</summary>
        static void Rehome(BarView bar, RectTransform column)
        {
            if (bar == null || column == null) return;
            var rt = bar.transform as RectTransform;
            if (rt == null || rt.parent == column) return;
            rt.SetParent(column, false);
            rt.SetAsFirstSibling();
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>Columns are placed from the top left, x across and y down, and their width
        /// never changes — only where they start and how far they fall.</summary>
        static void Place(RectTransform column, float x, float h)
        {
            if (column == null) return;
            column.anchoredPosition = new Vector2(x, -Layout.ColumnY);
            column.sizeDelta = new Vector2(column.sizeDelta.x, h);
        }

        static void Track(GameObject surface, StageProfile p)
        {
            if (surface == null) return;
            var rt = surface.transform as RectTransform;
            if (rt != null) rt.sizeDelta = new Vector2(rt.sizeDelta.x, p.TrackH);
        }

        /// <summary>The panel a column holds, as opposed to its bar: the direct child of the
        /// column that contains the given view.</summary>
        static GameObject PanelOf<T>(RectTransform column) where T : Component
        {
            if (column == null) return null;
            var view = column.GetComponentInChildren<T>(true);
            if (view == null) return null;
            var t = view.transform;
            while (t.parent != null && t.parent != column) t = t.parent;
            return t.parent == column ? t.gameObject : null;
        }

        /// <summary>The side columns are gated like any menu. Before its unlock a column's panel
        /// is absent; its bar, and Options and Exit in it, are never gated.</summary>
        void SyncColumns()
        {
            var s = WhatLiesInTheDepths.Data.GameState.I;
            if (s == null) return;
            Arrive(_ledgerPanel, s.LedgerOpen);
            Arrive(_gaugePanel, s.GaugeOpen);
            // The drawer's handles follow the panel they open: before the ledger arrives there
            // is nothing behind them, and a control for nothing is a promise.
            if (drawer != null) drawer.SetArrived(s.LedgerOpen, s.GaugeOpen);

            // The last place taken ends the story. It is shown once; Continue puts the dream back.
            if (s.unlocks.Has("gameover") && s.unlocks.Add("gameover:shown") && _router != null)
                _router.ShowEnding(true);
        }

        /// <summary>How long a panel takes to arrive: the ledger row's own fade-in, spec §2.</summary>
        const float ArriveSeconds = 0.6f;

        /// <summary>A panel appearing for the first time fades in rather than cutting in at full.</summary>
        void Arrive(GameObject panel, bool on)
        {
            if (panel == null || panel.activeSelf == on) return;
            panel.SetActive(on);
            if (!on) return;
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null) group = panel.AddComponent<CanvasGroup>();
            StartCoroutine(FadeIn(group));
        }

        static System.Collections.IEnumerator FadeIn(CanvasGroup g)
        {
            float t = 0f;
            g.alpha = 0f;
            while (t < ArriveSeconds && g != null)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / ArriveSeconds));
                yield return null;
            }
            if (g != null) g.alpha = 1f;
        }

        void Start()
        {
            _router = Router.I;
            if (_router == null) _router = gameObject.AddComponent<Router>();
            _router.Changed += Apply;
            _router.EndingShown += ShowEnding;
            if (endingGroup != null) endingGroup.alpha = 0f;
            if (ending != null) ending.SetActive(false);

            _ledgerPanel = PanelOf<LedgerView>(leftColumn);
            _gaugePanel = PanelOf<GaugeView>(rightColumn);
            var state = WhatLiesInTheDepths.Data.GameState.I;
            if (state != null) state.Changed += SyncColumns;
            SyncColumns();
            Apply();
        }

        void OnDestroy()
        {
            if (_router == null) return;
            _router.Changed -= Apply;
            _router.EndingShown -= ShowEnding;
            var state = WhatLiesInTheDepths.Data.GameState.I;
            if (state != null) state.Changed -= SyncColumns;
        }

        /// <summary>The center is the only column whose content changes. Switching
        /// destination touches nothing else — a dive keeps running, the ledger keeps
        /// counting, a channelling Vision keeps spending.</summary>
        void Apply()
        {
            bool onDestination = _router.Surface == CenterSurface.Destination;

            for (int i = 0; i < destinations.Count; i++)
            {
                var go = destinations[i];
                if (go == null) continue;
                bool on = onDestination && (int)_router.Destination == i;
                if (go.activeSelf != on) go.SetActive(on);
            }

            Set(optionsPage, _router.Surface == CenterSurface.Options);
            Set(exitQuestion, _router.Surface == CenterSurface.ExitQuestion);
            Set(hardResetQuestion, _router.Surface == CenterSurface.HardResetQuestion);
            Set(achievementsPage, _router.Surface == CenterSurface.Achievements);

            if (leftBar != null) leftBar.Refresh();
            if (centerBar != null) centerBar.Refresh();
            if (rightBar != null) rightBar.Refresh();
        }

        static void Set(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        /// <summary>It is a fade, not a cover. The columns go to 0 and stop taking the
        /// pointer; the ending comes up on the ground they were sitting on, so the
        /// interface's one hard rule survives the last screen intact.</summary>
        void ShowEnding(bool on)
        {
            // The ending is drawn over everything, so the drawer shuts and its tab goes with
            // it rather than sitting on the last screen of the game waiting to be pressed.
            if (drawer != null) drawer.Seal(on);
            if (ending != null) ending.SetActive(true);
            if (_endingFade != null) StopCoroutine(_endingFade);
            _endingFade = StartCoroutine(FadeRoutine(on));
        }

        System.Collections.IEnumerator FadeRoutine(bool on)
        {
            float t = 0f;
            float outDur = Layout.MotionColumnsOut;
            float inDur = Layout.MotionEndingIn;
            float dur = Mathf.Max(outDur, inDur);
            float fromCols = columnsGroup != null ? columnsGroup.alpha : 1f;
            float fromEnd = endingGroup != null ? endingGroup.alpha : 0f;

            while (t < dur)
            {
                t += Time.deltaTime;
                if (columnsGroup != null)
                {
                    columnsGroup.alpha = Mathf.Lerp(fromCols, on ? 0f : 1f, Mathf.Clamp01(t / outDur));
                    columnsGroup.blocksRaycasts = columnsGroup.alpha > 0.01f;
                    columnsGroup.interactable = columnsGroup.blocksRaycasts;
                }
                if (endingGroup != null)
                {
                    endingGroup.alpha = Mathf.Lerp(fromEnd, on ? 1f : 0f, Mathf.Clamp01(t / inDur));
                    endingGroup.blocksRaycasts = endingGroup.alpha > 0.01f;
                    endingGroup.interactable = endingGroup.blocksRaycasts;
                }
                yield return null;
            }

            if (!on && ending != null) ending.SetActive(false);
        }
    }
}
