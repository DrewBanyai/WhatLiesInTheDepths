// What Lies In The Depths — center destination. Spec section 4.
// The only destination that is purely read. Append-only; no counts, no search, no jump,
// no source filter. The chapters are the whole of the navigation.
using System.Collections.Generic;
using System.Linq;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class JournalView : MonoBehaviour
    {
        [Header("Chapter rail")]
        public RectTransform rail;
        public GameObject railItemPrefab;

        [Header("Scroll")]
        public ScrollRect scroll;
        public RectTransform content;
        public JournalEntryView entryPrefab;
        public GameObject endMarkPrefab;

        int _chapter = -1;
        readonly List<JournalEntryView> _views = new List<JournalEntryView>();
        readonly List<BarItem> _railItems = new List<BarItem>();

        int _written = -1;

        void OnEnable()
        {
            // Opening the Journal opens the latest chapter, not the one last read, because
            // the reason to open a journal is nearly always the thing that just happened.
            var s = GameState.I;
            if (s != null && s.journal.Count > 0)
                _chapter = s.journal.Max(e => e.ch);

            BuildRail();
            BuildEntries();
            _written = s != null ? s.journal.Count : 0;
            if (s != null) s.Changed += OnChanged;
        }

        void OnDisable()
        {
            if (GameState.I != null) GameState.I.Changed -= OnChanged;
        }

        /// <summary>The only destination that grows on its own: an entry written while the
        /// Journal is open appears in it, and a new chapter joins the rail.</summary>
        void OnChanged()
        {
            var s = GameState.I;
            if (s == null || s.journal.Count == _written) { RefreshRail(); return; }
            _written = s.journal.Count;
            BuildRail();
            BuildEntries();
        }

        void Update()
        {
            if (scroll == null) return;
            foreach (var v in _views) v.MarkSeenIfVisible(scroll.viewport);
        }

        /// <summary>Only chapters that have written something exist. The rail grows from one
        /// item to five across the game — five showing on the first night would tell the
        /// player how much dream is left, which is the number this interface never discloses.</summary>
        void BuildRail()
        {
            if (rail == null || railItemPrefab == null) return;
            Rebuild.Clear(rail);
            _railItems.Clear();

            var s = GameState.I;
            if (s == null) return;

            var present = s.journal.Select(e => e.ch).Distinct().OrderBy(c => c).ToList();
            foreach (var ch in present)
            {
                int captured = ch;
                var go = Instantiate(railItemPrefab, rail);
                var item = go.GetComponent<BarItem>();
                if (item != null && item.label != null)
                    item.label.text = Ursine.Text.Loc.Has("journal.chapter." + ch)
                        ? Strings.T("journal.chapter." + ch) : Strings.T("ui.journal.chapter");
                if (item != null && item.numeral != null) item.numeral.text = Fmt.Roman(ch);

                if (item != null)
                {
                    // A chapter is a place in the story, so it is set in the serif and
                    // marked with an underline exactly as a destination is.
                    item.Configure(
                        () => _chapter == captured,
                        () => s.journal.Any(e => e.ch == captured && !e.seen),
                        () => { _chapter = captured; BuildEntries(); RefreshRail(); });
                    _railItems.Add(item);
                }
            }
            RefreshRail();
        }

        void RefreshRail()
        {
            foreach (var i in _railItems) i.Refresh();
        }

        void BuildEntries()
        {
            if (content == null || entryPrefab == null) return;
            Rebuild.Clear(content);
            _views.Clear();

            var s = GameState.I;
            if (s == null) return;

            // Within a chapter, newest first.
            var entries = s.journal.Where(e => e.ch == _chapter).OrderByDescending(e => e.d).ToList();
            foreach (var e in entries)
            {
                var v = Instantiate(entryPrefab, content);
                v.Bind(e);
                _views.Add(v);
            }

            // The end mark only appears under the first chapter. Every other chapter simply
            // stops — the bottom of Chapter III is not the beginning of anything.
            bool endMark = endMarkPrefab != null && _chapter == 1;
            if (endMark) Instantiate(endMarkPrefab, content);
            // The spec closes every entry with a rule except the last one of the list; the
            // end mark counts as the list's last item, so above it the rule stays.
            if (!endMark && _views.Count > 0) _views[_views.Count - 1].SetRule(false);

            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
        }
    }
}
