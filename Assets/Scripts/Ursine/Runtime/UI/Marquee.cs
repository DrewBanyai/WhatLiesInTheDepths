// Ursine — a line that stands still when it fits and creeps when it does not.
//
// A marquee that scrolls whatever it is given is a fidget: most of what it shows would have
// sat still perfectly well. So the line is measured first, and only a line wider than the
// window it is read through ever moves. When it does move it is one unhurried pass, with the
// second copy already coming in behind it, so the loop never shows a window of nothing.
using TMPro;
using UnityEngine;

namespace Ursine.UI
{
    public sealed class Marquee : MonoBehaviour
    {
        /// <summary>The window the line is read through. Whatever masks it is the caller's
        /// business; this only measures against its width.</summary>
        public RectTransform viewport;

        public TMP_Text label;
        /// <summary>The second copy, following a gap behind the first. Shown only while
        /// scrolling, which is the only time there is anything for it to do.</summary>
        public TMP_Text echo;
        /// <summary>Faded out and in again when the line changes.</summary>
        public CanvasGroup group;

        public float pixelsPerSecond = 20f;
        /// <summary>Clear space between the end of one pass and the start of the next.</summary>
        public float gap = 64f;
        /// <summary>Still, at the start of every pass, so the beginning can be read.</summary>
        public float holdSeconds = 2.5f;
        public float fadeSeconds = 0.5f;

        string _wanted = string.Empty, _shown = string.Empty;
        float _width, _x, _hold, _alpha = 1f;
        bool _leaving;

        public string Text => _shown;

        /// <summary>The line to show. The same line twice is not a change and nothing
        /// happens; a different one fades this one out and the new one in.</summary>
        public void Show(string text)
        {
            text ??= string.Empty;
            if (text == _wanted) return;
            _wanted = text;

            if (string.IsNullOrEmpty(_shown)) { Put(text); _alpha = 0f; _leaving = false; }
            else _leaving = true;
        }

        void OnEnable() { Measure(); Place(); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float step = fadeSeconds <= 0f ? 1f : dt / fadeSeconds;

            if (_leaving)
            {
                _alpha = Mathf.MoveTowards(_alpha, 0f, step);
                if (_alpha <= 0f) { Put(_wanted); _leaving = false; }
            }
            else _alpha = Mathf.MoveTowards(_alpha, 1f, step);

            if (group != null) group.alpha = _alpha;

            Scroll(dt);
            Place();
        }

        void Scroll(float dt)
        {
            float window = viewport != null ? viewport.rect.width : 0f;
            if (window <= 0f || _width <= window) { _x = 0f; return; }

            if (_hold > 0f) { _hold -= dt; return; }

            _x -= pixelsPerSecond * dt;
            float span = _width + gap;
            if (_x <= -span) { _x += span; _hold = holdSeconds; }
        }

        void Put(string text)
        {
            _shown = text;
            if (label != null) label.text = text;
            if (echo != null) echo.text = text;
            _x = 0f;
            _hold = holdSeconds;
            Measure();
        }

        void Measure()
        {
            _width = label == null || string.IsNullOrEmpty(_shown)
                ? 0f
                : label.GetPreferredValues(_shown).x;
        }

        void Place()
        {
            float window = viewport != null ? viewport.rect.width : 0f;
            bool moving = window > 0f && _width > window;

            if (label != null)
            {
                var rt = (RectTransform)label.transform;
                rt.sizeDelta = new Vector2(Mathf.Max(_width, window), rt.sizeDelta.y);
                rt.anchoredPosition = new Vector2(_x, rt.anchoredPosition.y);
            }

            if (echo == null) return;
            echo.gameObject.SetActive(moving);
            if (!moving) return;
            var er = (RectTransform)echo.transform;
            er.sizeDelta = new Vector2(_width, er.sizeDelta.y);
            er.anchoredPosition = new Vector2(_x + _width + gap, er.anchoredPosition.y);
        }
    }
}
