// What Lies In The Depths — the stage. Fixed 1920 x 1080, three columns absolutely positioned,
// top-aligned, never stretched: the space between them is margin, not flex. Extra width
// goes to the outer margins and the columns stay put (spec section 1).
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using UnityEngine;
using UnityEngine.UI;

namespace WhatLiesInTheDepths.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenRoot : MonoBehaviour
    {
        [Header("Columns")]
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
