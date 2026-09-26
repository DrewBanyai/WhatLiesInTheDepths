// What Lies In The Depths — the left column. Spec section 2.
// The ledger is not interactive: no hover state, no tooltip, no focus ring, nothing to
// click. It is a readout and behaves like one — the only column on the screen the pointer
// has no business in.
using System.Collections.Generic;
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
    public sealed class LedgerView : MonoBehaviour
    {
        public RectTransform content;
        public ResourceRow rowPrefab;
        public GameObject groupCaptionPrefab;

        readonly List<ResourceRow> _rows = new List<ResourceRow>();
        string _built;

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var r in s.ShownResources) b.Append(r.k).Append(',');
            b.Append('|').Append(s.Version);
            return b.ToString();
        }

        void Start()
        {
            Build();
            if (GameState.I != null) GameState.I.Changed += RefreshAll;
            Theme.Changed += RefreshAll;
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= RefreshAll;
            Theme.Changed -= RefreshAll;
        }

        void Build()
        {
            var s = GameState.I;
            if (s == null || content == null || rowPrefab == null) return;

            Rebuild.Clear(content);
            _rows.Clear();
            _built = Signature();

            // Order within a group is authored and never re-sorts by amount, rate or
            // recency. Locked resources are absent — no grayed row, no question mark.
            // A row appearing is the only announcement that a resource exists.
            for (int g = 0; g < System.Enum.GetValues(typeof(ResGroup)).Length; g++)
            {
                var inGroup = s.resources.FindAll(r => r.g == g && s.Shown(r));
                if (inGroup.Count == 0) continue;

                if (groupCaptionPrefab != null)
                {
                    var cap = Instantiate(groupCaptionPrefab, content);
                    var t = cap.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = Strings.T("res.group." + g).ToUpperInvariant();
                }

                foreach (var r in inGroup)
                {
                    var row = Instantiate(rowPrefab, content);
                    row.Bind(r);
                    _rows.Add(row);
                }
            }

            Fit();
        }

        [Tooltip("The panel's height at its smallest: the spec's 501, which holds fifteen resources.")]
        public float minHeight = 501f;
        [Tooltip("Room above and below the rows inside the panel (the clip's 5 each side).")]
        public float padding = 10f;

        /// <summary>The panel grows with its rows, so the last resource is never cut off. It
        /// never shrinks below the spec's height, and never grows into the Now Playing line at the foot of the column.</summary>
        void Fit()
        {
            var panel = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float rows = LayoutUtility.GetPreferredHeight(content);
            // Its floor is the top of the Now Playing line at the foot of the column, with a
            // little mist between them.
            float most = Layout.ColumnH - Layout.NowPlayingH - (Layout.PanelTopY - Layout.ColumnY) - 16f;
            float h = Mathf.Clamp(Mathf.Ceil(rows + padding), minHeight, most);
            if (!Mathf.Approximately(panel.sizeDelta.y, h))
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, h);
        }

        void RefreshAll()
        {
            if (Signature() != _built) { Build(); return; }
            foreach (var r in _rows) r.Refresh();
        }
    }
}
