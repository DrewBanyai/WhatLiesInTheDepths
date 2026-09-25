// What Lies In The Depths — center destination. Spec section 5.
// At most one Focus is yours at a time, and the dive is part of the same single choice.
// Idleness is a position you can hold.
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
    public sealed class FocusView : MonoBehaviour
    {
        public TMP_Text poolLine;
        public RectTransform content;
        public GameObject sectionHeadPrefab;
        public FocusCardView cardPrefab;

        readonly List<FocusCardView> _cards = new List<FocusCardView>();
        string _built;

        static string Signature()
        {
            var s = GameState.I;
            if (s == null) return string.Empty;
            var b = new System.Text.StringBuilder();
            foreach (var t in s.ShownTasks) b.Append(t.id).Append(',');
            b.Append(s.BindingOpen).Append('|').Append(s.Version);
            return b.ToString();
        }

        void Start()
        {
            Build();
            if (GameState.I != null) GameState.I.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (GameState.I != null) GameState.I.Changed -= Refresh;
        }

        // The pool line's panel sits above the scroll; without it the cards take its place.
        RectTransform _scroll;
        Vector2 _scrollPos, _scrollSize;
        bool? _poolShown;

        void ShowPool(bool on)
        {
            if (_poolShown == on) return;
            _poolShown = on;
            var panel = poolLine.transform.parent != null ? poolLine.transform.parent.gameObject : poolLine.gameObject;
            if (panel != gameObject) panel.SetActive(on);

            if (_scroll == null && content != null)
            {
                var sr = content.GetComponentInParent<ScrollRect>(true);
                if (sr != null)
                {
                    _scroll = (RectTransform)sr.transform;
                    _scrollPos = _scroll.anchoredPosition;
                    _scrollSize = _scroll.sizeDelta;
                }
            }
            if (_scroll == null) return;
            float lift = on ? 0f : Mathf.Abs(_scrollPos.y);
            _scroll.anchoredPosition = new Vector2(_scrollPos.x, _scrollPos.y + (on ? 0f : lift));
            _scroll.sizeDelta = new Vector2(_scrollSize.x, _scrollSize.y + lift);
        }

        static RectTransform NewNode(string name, Transform parent)
        {
            var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rt.SetParent(parent, false);
            return rt;
        }

        void Build()
        {
            var s = GameState.I;
            if (s == null || content == null || cardPrefab == null) return;

            Rebuild.Clear(content);
            _cards.Clear();
            _built = Signature();

            // Locked focuses are absent, and a section appears with its first card.
            foreach (var section in s.ShownTasks.Select(t => t.section).Distinct())
            {
                if (sectionHeadPrefab != null)
                {
                    var head = Instantiate(sectionHeadPrefab, content);
                    var t = head.GetComponentInChildren<TMP_Text>();
                    if (t != null) t.text = section.ToUpperInvariant();
                }

                // Two columns, 16 gutter. Each row is as tall as the taller of its pair, as
                // the spec's grid stretches them; a row never takes another row's height.
                var grid = NewNode(section + " grid", content);
                var rows = grid.gameObject.AddComponent<VerticalLayoutGroup>();
                rows.spacing = 16f;
                rows.childControlWidth = rows.childControlHeight = true;
                rows.childForceExpandWidth = rows.childForceExpandHeight = false;

                var cardSize = PrefabRect.Size(cardPrefab);
                RectTransform row = null;
                int inRow = 0;
                foreach (var task in s.ShownTasks.Where(t => t.section == section))
                {
                    if (row == null || inRow == 2)
                    {
                        row = NewNode("Row", grid);
                        var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                        h.spacing = 16f;
                        h.childControlWidth = h.childControlHeight = true;
                        h.childForceExpandWidth = false;
                        h.childForceExpandHeight = true;
                        inRow = 0;
                    }
                    var card = Instantiate(cardPrefab, row);
                    card.Bind(task);
                    _cards.Add(card);
                    var le = card.GetComponent<LayoutElement>();
                    if (le == null) le = card.gameObject.AddComponent<LayoutElement>();
                    le.preferredWidth = cardSize.x;
                    le.preferredHeight = card.PreferredHeight;
                    inRow++;
                }
            }
        }

        void Refresh()
        {
            var s = GameState.I;
            if (s == null) return;
            if (Signature() != _built) Build();

            // The pool line states the Oneiri total once for the whole menu, so no card has
            // to explain why its plus is gray. What you have not unlocked does not exist, and
            // that holds for parts of a menu too: no binding, no pool line.
            if (poolLine != null)
            {
                ShowPool(s.BindingOpen);
                poolLine.text = Strings.T("ui.focus.pool", Fmt.Count(s.OneiriFree), Fmt.Count(s.OneiriTotal));
            }

            foreach (var c in _cards) c.Refresh();
        }
    }
}
