// What Lies In The Depths — one Journal entry. Spec section 4.
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
    public sealed class JournalEntryView : MonoBehaviour
    {
        public AttentionDot dot;
        public Image sourceGlyph;
        public TMP_Text sourceWord;
        public TMP_Text dayLabel;
        public TMP_Text dayNumber;
        public TMP_Text entryName;
        public RectTransform paragraphs;
        public GameObject paragraphPrefab;
        public Image rule;

        JournalEntry _e;


        public void Bind(JournalEntry e)
        {
            _e = e;
            // The source mark is the destination's own glyph, learned before the Journal
            // is ever opened; the veil has its own.
            Art.Apply(sourceGlyph, Art.Source(e.src));
            if (dayLabel != null) dayLabel.text = Strings.T("ui.journal.day").ToUpperInvariant();
            if (dayNumber != null) dayNumber.text = Fmt.Count(e.d);
            if (entryName != null) entryName.text = e.n;   // the triggering thing's own name

            if (sourceWord != null)
            {
                string src = e.src ?? string.Empty;
                string w = Ursine.Text.Loc.Has("ui.journal.source." + src)
                    ? Strings.T("ui.journal.source." + src) : Strings.T("ui.journal.source.other");
                if (e.great && Ursine.Text.Loc.Has("ui.journal.source.greater." + src))
                    w = Strings.T("ui.journal.source.greater", Strings.T("ui.journal.source.greater." + src));
                sourceWord.text = w.ToUpperInvariant();
                // Gold already means the greater tier in both menus, so this adds no new reading.
                sourceWord.color = Theme.Get(e.great ? Tok.GoldD : Tok.Ink3);
            }
            if (sourceGlyph != null) sourceGlyph.color = Theme.Get(e.great ? Tok.GoldD : Tok.Ink3);
            if (dot != null) dot.Set(!e.seen);

            // Paragraphs follow the name in the body's own column, so only the ones this
            // view made are cleared on a rebind — the source line and the name stay.
            if (paragraphs != null && paragraphPrefab != null)
            {
                foreach (var go in _paragraphs) if (go != null) Destroy(go);
                _paragraphs.Clear();
                foreach (var p in e.p)
                {
                    var go = Instantiate(paragraphPrefab, paragraphs);
                    var t = go.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = p;
                    _paragraphs.Add(go);
                }
            }
        }

        readonly List<GameObject> _paragraphs = new List<GameObject>();

        /// <summary>The last entry of a chapter with nothing under it drops its rule.</summary>
        public void SetRule(bool on)
        {
            if (rule != null) rule.gameObject.SetActive(on);
        }

        /// <summary>An entry's dot clears when the entry has been on screen, not when it is
        /// clicked — there is nothing to click. The threshold is a guess, and the spec says so.</summary>
        public void MarkSeenIfVisible(RectTransform viewport)
        {
            if (_e == null || _e.seen || viewport == null) return;
            var rt = (RectTransform)transform;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var vp = new Vector3[4];
            viewport.GetWorldCorners(vp);
            bool visible = corners[0].y < vp[2].y && corners[2].y > vp[0].y;
            if (!visible) return;
            _e.seen = true;
            if (dot != null) dot.Set(false);
        }
    }
}
