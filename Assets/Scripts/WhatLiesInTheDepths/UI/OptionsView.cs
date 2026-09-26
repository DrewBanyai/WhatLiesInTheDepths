// What Lies In The Depths — the two pages that are not part of the dream. Spec section 10.
// Every control takes effect immediately. No Apply, no Save, no Cancel. Neither page is
// dismissed by clicking away: there is no away.
using System.Collections.Generic;
using WhatLiesInTheDepths.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Palette = Ursine.Theming.Palette;
using Ursine.Audio;
using Ursine.UI;
using Ursine;

namespace WhatLiesInTheDepths.UI
{
    public sealed class OptionsView : MonoBehaviour
    {
        [Header("Sound")]
        public VolumeBar master;
        public VolumeBar music;
        public VolumeBar effects;
        public TMP_Text masterValue;
        public TMP_Text musicValue;
        public TMP_Text effectsValue;
        public UiButton mute;
        public Image muteBox;
        public CanvasGroup soundRows;

        [Header("Display")]
        public ToggleSwitch fullScreen;
        public List<UiButton> paletteCards = new List<UiButton>();
        public List<Image> paletteBorders = new List<Image>();
        public List<Palette> palettes = new List<Palette>();
        public SegmentedToggle contrast;

        [Header("The save")]
        public TMP_Text saveLine;
        public UiButton save;
        public Image saveBorder;
        public TMP_Text saveLabel;
        float _savedFor;

        [Header("Beginning again")]
        public UiButton hardReset;
        public Image hardResetBlock;

        bool _muted;
        bool _repaint;

        // A palette or contrast change sends every bound graphic back to its token, which
        // would put the selection mark back on the first card and clear the mute box. So the
        // page's own marks are drawn again after any theme change, once everything else has
        // taken it.
        void OnEnable() { Theme.Changed += AskRepaint; _repaint = true; }
        void OnDisable() => Theme.Changed -= AskRepaint;
        void AskRepaint() => _repaint = true;

        void LateUpdate()
        {
            if (!_repaint) return;
            _repaint = false;
            _muted = GameSettings.Muted;
            PaintMute();
            PaintPalettes(GameSettings.PaletteIndex);
            PaintSave();
        }

        void Start()
        {
            // The page opens showing what is in effect — what was saved, or the defaults —
            // rather than what the builder happened to draw.
            if (master != null) master.Set(GameSettings.Master, false);
            if (music != null) music.Set(GameSettings.Music, false);
            if (effects != null) effects.Set(GameSettings.Effects, false);
            _muted = GameSettings.Muted;
            PaintMute();
            if (contrast != null) contrast.SetSilently((int)GameSettings.ContrastLevel);
            if (fullScreen != null) fullScreen.Set(Screen.fullScreen, false);

            Hook(master, masterValue);
            Hook(music, musicValue);
            Hook(effects, effectsValue);

            if (mute != null) mute.Clicked += () =>
            {
                _muted = !_muted;
                PaintMute();
                GameSettings.SetMuted(_muted);
            };

            for (int i = 0; i < paletteCards.Count; i++)
            {
                int captured = i;
                if (paletteCards[i] != null)
                    paletteCards[i].Clicked += () => ChoosePalette(captured);
            }

            if (contrast != null)
                contrast.Selected += i => GameSettings.SetContrast((Contrast)Mathf.Clamp(i, 0, 2));

            if (save != null)
            {
                // Saves at once and restarts the two-minute count; the button says so briefly.
                save.Clicked += () =>
                {
                    Data.GameState.I?.SaveNow();
                    _savedFor = 1.5f;
                    PaintSave();
                };
                save.Hovered += _ => PaintSave();
            }
            PaintSave();

            if (hardReset != null) hardReset.Clicked += () => Router.I?.AskHardReset();

            if (fullScreen != null)
                fullScreen.Changed += on => Screen.fullScreen = on;

            PaintPalettes(GameSettings.PaletteIndex);
        }

        /// <summary>Muted, the three rows above drop to 40% and stop responding, and their
        /// values are kept — unmuting restores exactly what was there.</summary>
        void PaintMute()
        {
            if (soundRows != null)
            {
                soundRows.alpha = _muted ? 0.4f : 1f;
                soundRows.blocksRaycasts = !_muted;
                soundRows.interactable = !_muted;
            }
            if (muteBox != null) muteBox.color = Theme.Get(_muted ? Tok.Iris : Tok.Track);
        }

        /// <summary>A bar, its readout beside it, and the music: all three move on the frame
        /// of the drag, because that is the whole rule of this page.</summary>
        void Hook(VolumeBar bar, TMP_Text readout)
        {
            if (bar == null) return;
            bar.Changed += _ => { Readout(bar, readout); Hear(); };
            Readout(bar, readout);
        }

        static void Readout(VolumeBar bar, TMP_Text readout)
        {
            if (readout != null) readout.text = Fmt.Percent(bar.Value * 100);
        }

        /// <summary>Master multiplies, it does not override: a master at half and music at
        /// half is a quarter, which is what a player setting both to half means. Effects has
        /// nothing to carry yet, so nothing listens to it.</summary>
        void Hear()
        {
            GameSettings.SetVolumes(master != null ? master.Value : GameSettings.Master,
                                    music != null ? music.Value : GameSettings.Music,
                                    effects != null ? effects.Value : GameSettings.Effects);
        }

        void Update()
        {
            // The save clock is real. Staleness is given in whole seconds, always.
            if (saveLine != null && SaveClock.I != null)
                saveLine.text = Strings.T("ui.options.saved", Fmt.Seconds(SaveClock.I.Staleness));

            if (_savedFor > 0f && (_savedFor -= Time.unscaledDeltaTime) <= 0f) PaintSave();
        }

        void PaintSave()
        {
            bool just = _savedFor > 0f;
            bool hover = save != null && save.IsHovered;
            if (saveLabel != null)
            {
                saveLabel.text = Strings.T(just ? "ui.options.savedNow" : "ui.options.save");
                saveLabel.color = Theme.Get(just ? Tok.TealD : Tok.IrisD);
            }
            if (saveBorder != null) saveBorder.color = Theme.Get(just ? Tok.TealD : hover ? Tok.Iris : Tok.IrisB);
        }

        /// <summary>A palette is a whole token set, not a filter. Switching one rewrites all
        /// twenty-eight values at once, and anything written as a literal hex will not follow.</summary>
        void ChoosePalette(int index)
        {
            GameSettings.SetPalette(index);
            PaintPalettes(GameSettings.PaletteIndex);
        }

        void PaintPalettes(int index)
        {
            for (int i = 0; i < paletteBorders.Count; i++)
                if (paletteBorders[i] != null)
                    paletteBorders[i].color = Theme.Get(i == index ? Tok.Iris : Tok.Haze);
        }
    }
}
