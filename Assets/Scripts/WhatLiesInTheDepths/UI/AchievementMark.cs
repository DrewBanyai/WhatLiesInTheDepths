// What Lies In The Depths — one mark on the Achievements page.
// Blank until earned: block ground, haze edge, no glyph. Earned: iris-l ground, iris-b edge,
// the mark's own glyph in iris-d. Resting the pointer on it reads it out in the band.
using System;
using UnityEngine;
using UnityEngine.UI;
using Ursine.UI;
using WhatLiesInTheDepths.Core;
using WhatLiesInTheDepths.Data;

namespace WhatLiesInTheDepths.UI
{
    public sealed class AchievementMark : MonoBehaviour
    {
        public UiButton button;
        public Image ground;
        public Image border;
        public Image glyph;

        [NonSerialized] public AchievementDef def;
        public event Action<bool> Rested;

        bool _earned;
        bool _hovered;

        public void Bind(AchievementDef a, bool earned, Sprite sprite)
        {
            def = a;
            _earned = earned;
            if (glyph != null)
            {
                Art.Apply(glyph, sprite);
                glyph.enabled = earned && glyph.sprite != null;
            }
            if (button != null)
                button.Hovered += h => { _hovered = h; Paint(); Rested?.Invoke(h); };
            Paint();
        }

        void Paint()
        {
            if (ground != null) ground.color = Theme.Get(_earned ? Tok.IrisL : Tok.Block);
            if (border != null)
                border.color = Theme.Get(_earned ? Tok.IrisB : (_hovered ? Tok.Ink4 : Tok.Haze));
            if (glyph != null) glyph.color = Theme.Get(Tok.IrisD);
        }
    }
}
