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
        }

        void RefreshAll()
        {
            if (Signature() != _built) { Build(); return; }
            foreach (var r in _rows) r.Refresh();
        }
    }
}
