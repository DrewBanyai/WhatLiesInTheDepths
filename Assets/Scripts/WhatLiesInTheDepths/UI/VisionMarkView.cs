// What Lies In The Depths — one Vision mark, carrying its progress on its rim. Spec section 8.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class VisionMarkView : MonoBehaviour
    {
        public UiButton button;
        public Image glyph;
        public Image ringTrack;
        public Image ringFill;
        public GameObject channelDot;
        public CanvasGroup group;

        public VisionDef Def { get; private set; }
        public System.Action<VisionMarkView, bool> HoverChanged;

        float _pulse;

        public void Bind(VisionDef d)
        {
            Def = d;
            Art.Apply(glyph, Art.Vision(d.k));
            if (button != null) button.Hovered += h => HoverChanged?.Invoke(this, h);
            Refresh();
        }

        public void Refresh()
        {
            if (Def == null) return;
            if (ringTrack != null) ringTrack.color = Theme.Get(Tok.Track);
            if (ringFill != null)
            {
                ringFill.type = Image.Type.Filled;
                ringFill.fillMethod = Image.FillMethod.Radial360;
                ringFill.fillOrigin = 2;                       // top
                ringFill.fillAmount = Mathf.Clamp01(Def.p / 100f);
                ringFill.color = Theme.Get(Def.great ? Tok.Gold : Tok.Iris);
            }
            if (glyph != null) glyph.color = Theme.Get(Def.great ? Tok.GoldD : Tok.IrisD);
            if (channelDot != null) channelDot.SetActive(Def.a);
        }

        void Update()
        {
            if (Def == null) return;
            // A channelling mark carries a pulsing 9px iris dot in the field so the drain
            // is never invisible.
            if (Def.a && channelDot != null)
            {
                _pulse += Time.deltaTime * 2.2f;
                var img = channelDot.GetComponent<Image>();
                if (img != null) img.color = Theme.Get(Tok.Iris, 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(_pulse)));
            }
            if (ringFill != null) ringFill.fillAmount = Mathf.Clamp01(Def.p / 100f);
        }
    }
}
