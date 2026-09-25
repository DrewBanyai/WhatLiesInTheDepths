// What Lies In The Depths — Achievements. The marks the dream has left, and the ones it has not.
//
// A reading band across the top, then the marks in their groups, eight to a row. The band
// reads out the count at rest, and whichever mark the pointer rests on otherwise: a blank
// mark gives only its hint, an earned one its name and what earned it. Nothing here is
// pressed; the page is read.
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;

namespace WhatLiesInTheDepths.UI
{
    public sealed class AchievementsView : MonoBehaviour
    {
        [Header("The band")]
        public Image bandGround;
        public Image bandBorder;
        public Image markGround;
        public Image markBorder;
        public Image markGlyph;
        public TMP_Text kicker;
        public TMP_Text title;
        public TMP_Text body;
        public TMP_Text hint;

        [Header("The marks")]
        public RectTransform content;
        public GameObject groupHeadPrefab;
        public AchievementMark markPrefab;

        [Header("Measure")]
        public float left = 34f;
        public float top = 172f;
        public float width = 812f;
        public float mark = 84f;
        public float gap = 20f;
        public float headH = 30f;
        public float groupGap = 30f;

        Sprite _grid;
        string _signature;
        AchievementDef _resting;
        readonly List<AchievementMark> _marks = new List<AchievementMark>();

        void OnEnable()
        {
            if (markGlyph != null && _grid == null) _grid = markGlyph.sprite;
            if (GameState.I != null) GameState.I.Changed += Refresh;
            _resting = null;
            _signature = null;
            Refresh();
        }

        void OnDisable()
        {
            if (GameState.I != null) GameState.I.Changed -= Refresh;
        }

        void Refresh()
        {
            var s = GameState.I;
            if (s == null) return;
            string sig = string.Join(",", s.achievements.Select(a => a.k + (s.Earned(a) ? "+" : "-")));
            if (sig != _signature)
            {
                _signature = sig;
                Draw(s);
            }
            Band(s);
        }

        void Draw(GameState s)
        {
            Rebuild.Clear(content);
            _marks.Clear();
            if (content == null) return;

            int perRow = Mathf.Max(1, Mathf.FloorToInt((width + gap) / (mark + gap)));
            float y = top;

            // Groups in the order their first mark was written.
            var groups = new List<string>();
            foreach (var a in s.achievements) if (!groups.Contains(a.group)) groups.Add(a.group);

            foreach (var g in groups)
            {
                var inGroup = s.achievements.Where(a => a.group == g).ToList();
                int earned = inGroup.Count(s.Earned);

                if (groupHeadPrefab != null)
                {
                    var head = Instantiate(groupHeadPrefab, content);
                    Place((RectTransform)head.transform, left, y);
                    SetText(head.transform, "Name", g.ToUpperInvariant());
                    SetText(head.transform, "Count", Strings.T("ui.achievements.count", earned, inGroup.Count));
                }
                y += headH;

                for (int i = 0; i < inGroup.Count; i++)
                {
                    if (markPrefab == null) break;
                    var a = inGroup[i];
                    var m = Instantiate(markPrefab, content);
                    Place((RectTransform)m.transform, left + (i % perRow) * (mark + gap),
                          y + (i / perRow) * (mark + gap));
                    m.Bind(a, s.Earned(a), Art.Achievement(a.glyph));
                    m.Rested += on => { _resting = on ? m.def : (_resting == m.def ? null : _resting); Band(GameState.I); };
                    _marks.Add(m);
                }
                int rows = Mathf.CeilToInt(inGroup.Count / (float)perRow);
                y += rows * mark + Mathf.Max(0, rows - 1) * gap + groupGap;
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, y);
        }

        static void Place(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        static void SetText(Transform root, string name, string text)
        {
            var t = Rebuild.Deep(root, name);
            var tmp = t != null ? t.GetComponent<TMP_Text>() : null;
            if (tmp != null) tmp.text = text;
        }

        /// <summary>The reading band. At rest: the count. On a blank mark: "Not yet" and its
        /// hint. On an earned one: its name, and what earned it.</summary>
        void Band(GameState s)
        {
            if (s == null) return;
            var a = _resting;
            bool earned = a != null && s.Earned(a);

            if (bandGround != null) bandGround.color = Theme.Get(earned ? Tok.IrisL : Tok.Veil);
            if (bandBorder != null) bandBorder.color = Theme.Get(earned ? Tok.IrisB : Tok.Haze);
            if (markGround != null) markGround.color = Theme.Get(earned ? Tok.IrisL : Tok.Block);
            if (markBorder != null) markBorder.color = Theme.Get(earned ? Tok.IrisB : Tok.Haze2);
            if (markGlyph != null)
            {
                Sprite own = a != null ? Art.Achievement(a.glyph) : null;
                markGlyph.sprite = a == null ? _grid : (earned ? own ?? _grid : _grid);
                markGlyph.color = Theme.Get(earned ? Tok.IrisD : Tok.Ink3);
                markGlyph.enabled = a == null || earned;
            }

            if (a == null)
            {
                int n = s.achievements.Count(s.Earned);
                Show(Strings.T("ui.achievements.title"), null,
                     Strings.T("ui.achievements.rest", n, s.achievements.Count)
                     + " <i>" + Strings.T("ui.achievements.restHint") + "</i>", null);
            }
            else if (earned)
                Show(Strings.T("ui.achievements.earned"), a.n, a.req, null);
            else
                Show(Strings.T("ui.achievements.notYet"), null, null, a.hint);
        }

        void Show(string kick, string name, string line, string hintLine)
        {
            if (kicker != null) kicker.text = (kick ?? "").ToUpperInvariant();
            if (title != null)
            {
                title.gameObject.SetActive(!string.IsNullOrEmpty(name));
                title.text = name ?? "";
            }
            if (body != null)
            {
                body.gameObject.SetActive(!string.IsNullOrEmpty(line));
                body.text = line ?? "";
                // With no name above it, the line rises into the name's place.
                var rt = (RectTransform)body.transform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, string.IsNullOrEmpty(name) ? -44f : -74f);
            }
            if (hint != null)
            {
                hint.gameObject.SetActive(!string.IsNullOrEmpty(hintLine));
                hint.text = hintLine ?? "";
            }
        }
    }
}
