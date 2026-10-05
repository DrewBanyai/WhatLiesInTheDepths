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
    public sealed class OptionsView : MonoBehaviour, IStageFit
    {
        [Tooltip("The column of rows. Inset from the top of the track when there is room, and "
               + "sitting nearer it when there is not.")]
        public RectTransform page;

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
        [Tooltip("How large the whole page is drawn, as a fraction of what fits on the "
               + "screen. The top of the bar is the fit itself; there is nothing above it.")]
        public VolumeBar uiScale;
        public TMP_Text uiScaleValue;
        [Tooltip("How much larger than authored the small type is drawn. The readout is the "
               + "rate the captions move at; prose moves at a quieter one of its own.")]
        public VolumeBar textSize;
        public TMP_Text textSizeValue;
        public List<UiButton> paletteCards = new List<UiButton>();
        public List<Image> paletteBorders = new List<Image>();
        public List<Palette> palettes = new List<Palette>();
        public SegmentedToggle contrast;
        [Tooltip("Auto / Full / Compact. Auto is what almost everyone wants; the other two "
               + "are for a player who would rather not resize their window to argue with us.")]
        public SegmentedToggle layout;

        [Header("The save")]
        public TMP_Text saveLine;
        public UiButton save;
        public Image saveBorder;
        public TMP_Text saveLabel;
        float _savedFor;

        [Tooltip("Export a copy of the save to a file; Import one back. The line says what happened.")]
        public UiButton exportSave;
        public Image exportBorder;
        public UiButton importSave;
        public Image importBorder;
        public TMP_Text transferLine;

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

        /// <summary>The page is a fixed column of rows — about 754 of them with the Layout
        /// control in, against a short track of 765. In the tall stage it is inset 60 and has
        /// air beneath it; in the short one the air is what gives, and the page simply sits
        /// higher. Nothing reflows: it is the same page.
        ///
        /// The margin there is thin enough that one more row would push it off the bottom, so
        /// rather than leave that to a number someone has to remember to update, the page is
        /// measured and taken down by however much it is over — which today is nothing, and if
        /// it ever is something it will be a percent or two rather than a row gone missing.</summary>
        public void Fit(StageProfile profile)
        {
            if (page == null) return;
            float inset = profile.IsShort ? 10f : 60f;
            float room = profile.TrackH - inset * 2f;

            page.anchoredPosition = new Vector2(page.anchoredPosition.x, -inset);
            page.sizeDelta = new Vector2(page.sizeDelta.x, room);

            float need = Extent();
            float k = need > room && need > 1f ? room / need : 1f;
            page.localScale = new Vector3(k, k, 1f);
        }

        /// <summary>How far the lowest row reaches. The page's children are placed absolutely
        /// from its head, so this is the page's real height however many rows it grows.</summary>
        float Extent()
        {
            float most = 0f;
            for (int i = 0; i < page.childCount; i++)
            {
                var c = page.GetChild(i) as RectTransform;
                if (c == null || !c.gameObject.activeSelf) continue;
                float bottom = -c.anchoredPosition.y + c.rect.height;
                if (bottom > most) most = bottom;
            }
            return most;
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
            if (layout != null) layout.SetSilently((int)GameSettings.LayoutMode);
            if (fullScreen != null) fullScreen.Set(Screen.fullScreen, false);
            if (uiScale != null) uiScale.Set(ScaleToBar(GameSettings.UiScale), false);
            if (textSize != null) textSize.Set(GameSettings.TextSize, false);
            PaintUiScale();
            PaintTextSize();

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

            if (layout != null)
                layout.Selected += i => GameSettings.SetLayoutMode((StageMode)Mathf.Clamp(i, 0, 3));

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

            if (transferLine != null) transferLine.text = Strings.T("ui.options.transfer");
            if (exportSave != null)
            {
                exportSave.Clicked += Export;
                exportSave.Hovered += h => { if (exportBorder != null) exportBorder.color = Theme.Get(h ? Tok.Iris : Tok.IrisB); };
            }
            if (importSave != null)
            {
                importSave.Clicked += Import;
                importSave.Hovered += h => { if (importBorder != null) importBorder.color = Theme.Get(h ? Tok.Iris : Tok.IrisB); };
            }

            if (fullScreen != null)
                fullScreen.Changed += on => Screen.fullScreen = on;

            if (uiScale != null)
                uiScale.Changed += v =>
                {
                    GameSettings.SetUiScale(BarToScale(v));
                    PaintUiScale();
                };

            // The bar is the amount, 0 to 1, so it needs no mapping of its own.
            if (textSize != null)
                textSize.Changed += v =>
                {
                    GameSettings.SetTextSize(v);
                    PaintTextSize();
                };

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

        // ---- a save the player can hold ----------------------------------------------------

        void Export()
        {
            var s = Data.GameState.I;
            if (s == null) return;
            var outcome = s.ExportSave(out string where);
            switch (outcome)
            {
                case SaveTransfer.Outcome.Done:
                    // The web hands the browser a download; everywhere else it is a file we wrote.
                    Say(where == null ? Strings.T("ui.options.exportedWeb")
                                      : Strings.T("ui.options.exported", System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(where))),
                        Tok.TealD);
                    break;
                case SaveTransfer.Outcome.Cancelled: Say(Strings.T("ui.options.transfer"), Tok.Ink3); break;
                default: Say(Strings.T("ui.options.exportFailed"), Tok.RoseD); break;
            }
        }

        void Import()
        {
            var s = Data.GameState.I;
            if (s == null) return;
            s.ImportSave(result =>
            {
                switch (result)
                {
                    case Data.GameState.ImportResult.Loaded: Say(Strings.T("ui.options.imported"), Tok.TealD); break;
                    case Data.GameState.ImportResult.NoneFound: Say(Strings.T("ui.options.importNone", SaveTransfer.FolderName), Tok.RoseD); break;
                    case Data.GameState.ImportResult.NotASave: Say(Strings.T("ui.options.importBad"), Tok.RoseD); break;
                    default: Say(Strings.T("ui.options.transfer"), Tok.Ink3); break;
                }
            });
        }

        void Say(string text, Tok tone)
        {
            if (transferLine == null) return;
            transferLine.text = text;
            transferLine.color = Theme.Get(tone);
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

        // The bar runs the whole way across, but the stage only goes from the smallest size
        // worth offering up to the fit, so the two are mapped onto each other rather than
        // the bar being left mostly unusable at its left end.
        static float BarToScale(float bar) => Mathf.Lerp(FixedStage.MinScale, 1f, Mathf.Clamp01(bar));
        static float ScaleToBar(float scale) => Mathf.InverseLerp(FixedStage.MinScale, 1f, scale);

        void PaintUiScale()
        {
            if (uiScaleValue != null) uiScaleValue.text = Fmt.Percent(GameSettings.UiScale * 100);
        }

        void PaintTextSize()
        {
            if (textSizeValue != null) textSizeValue.text = Fmt.Percent(Ursine.Text.TextScale.Percent);
        }

        /// <summary>A palette is a whole token set, not a filter. Switching one rewrites all
        /// thirty-two values at once, and anything written as a literal hex will not follow.</summary>
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
