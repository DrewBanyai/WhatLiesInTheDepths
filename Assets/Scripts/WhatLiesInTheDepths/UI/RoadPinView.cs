// What Lies In The Depths — one place on the Assault road. Spec section 9.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using Ursine.UI;

namespace WhatLiesInTheDepths.UI
{
    public sealed class RoadPinView : MonoBehaviour
    {
        public UiButton button;
        public Image glow, dot, ring, glyph;
        public TMP_Text label;
        public Sprite ring30, ring38;

        public RoadLocation Place { get; private set; }
        public System.Action<RoadPinView> Rested;

        // The spec's raw values where it does not use a token.
        static readonly Color TakenRing = new Color32(0xA8, 0xD2, 0xCC, 0xFF);

        bool _hover, _on, _fighting;

        public void Bind(RoadLocation place)
        {
            Place = place;
            ((RectTransform)transform).anchoredPosition = Road.Anchored(place.t);
            if (label != null) label.text = place.n.ToUpperInvariant();
            if (button != null)
                button.Hovered += h =>
                {
                    _hover = h;
                    if (h) Rested?.Invoke(this);    // resting on a pin reads it; leaving keeps it
                    Paint();
                };
            Paint();
        }

        public void SetOn(bool on) { _on = on; Paint(); }
        public void SetFighting(bool fighting) { _fighting = fighting; if (!fighting) Paint(); }

        void Update()
        {
            if (!_fighting || glow == null) return;
            // The pin being fought over breathes: a 5px glow at .13 out to 11px at .05.
            float u = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / 1.1f * Mathf.PI * 2f);
            float size = Place.won ? 30f : 38f;
            float spread = Mathf.Lerp(5f, 11f, u);
            glow.rectTransform.sizeDelta = Vector2.one * (size + spread * 2f);
            glow.color = Theme.Get(Tok.Iris, Mathf.Lerp(0.13f, 0.05f, u));
        }

        void Paint()
        {
            if (Place == null) return;
            bool won = Place.won;
            float size = won ? 30f : 38f;
            if (dot != null)
            {
                dot.rectTransform.sizeDelta = Vector2.one * size;
                dot.color = won ? Theme.Get(Tok.TealL) : Color.white;
            }
            if (ring != null)
            {
                ring.rectTransform.sizeDelta = Vector2.one * size;
                // 1.5px round a taken dot, 2px round the next one.
                var sprite = won ? ring30 : ring38;
                if (sprite != null) ring.sprite = sprite;
                ring.color = _on ? Theme.Get(Tok.Iris)
                           : _hover ? Theme.Get(Tok.IrisB)
                           : won ? TakenRing : Theme.Get(Tok.Iris);
            }
            if (glow != null)
            {
                // Next: a 5px glow at .13; the one being read: 5px at .18; a taken one only
                // when it is being read.
                bool show = !won || _on;
                glow.gameObject.SetActive(show);
                glow.rectTransform.sizeDelta = Vector2.one * (size + 10f);
                glow.color = Theme.Get(Tok.Iris, _on ? 0.18f : 0.13f);
            }
            if (glyph != null)
            {
                Art.Apply(glyph, won ? Art.Ui("check") : Art.Place(Place.k));
                glyph.rectTransform.sizeDelta = Vector2.one * (won ? 14f : 18f);
                glyph.color = Theme.Get(won ? Tok.TealD : Tok.IrisD);
            }
            if (label != null)
            {
                label.color = Theme.Get(_on || !won ? Tok.Ink : Tok.TealD);
                var rt = label.rectTransform;
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -(19f + size * 0.5f + 5f));
            }
        }
    }
}
