// What Lies In The Depths — the Options page's choices, kept between sessions.
//
// Settings belong to the player on this device, not to the dream: they are kept in a save of
// their own ("settings", beside "dream" — see Ursine.SaveStore for where that is on each
// platform), so a hard reset starts the dream over without turning the music back up.
//
// Every control on the Options page takes effect immediately, so every change is saved
// without being asked: a change asks for a save, and the save follows half a second after
// the last one, so dragging a volume bar writes once when the hand stops rather than on
// every frame of the drag. The dream's own saves (every two minutes, Save, leaving the game)
// write the settings too.
using System;
using System.Globalization;
using UnityEngine;
using Ursine;
using Ursine.Audio;
using Ursine.Text;
using Ursine.UI;
using Palette = Ursine.Theming.Palette;

namespace WhatLiesInTheDepths.Core
{
    public static class GameSettings
    {
        public const string Slot = "settings";

        /// <summary>The palettes in the order the Options page shows them.</summary>
        public static readonly string[] PaletteNames = { "Dream", "Dusk", "Parchment" };

        public static float Master = Layout.VolumeMaster;
        public static float Music = Layout.VolumeMusic;
        public static float Effects = Layout.VolumeEffects;
        public static bool Muted;
        public static int PaletteIndex;
        public static Contrast ContrastLevel = Contrast.Soft;

        /// <summary>The player's size for the stage, as a fraction of what fits on their
        /// screen. One is the whole design drawn as large as it will go without losing an
        /// edge, which is where everyone starts; below one is a smaller picture with more
        /// room around it, which is a comfort on a very large monitor. There is nothing
        /// above one: the fit is already the largest the design can be drawn.</summary>
        public static float UiScale = 1f;

        /// <summary>Which shape the stage is in. Auto is the answer for almost everyone —
        /// it reads the window and picks — and the other two are here because a player who
        /// wants the whole three-column tableau on a small screen, or the larger type on a
        /// big one, should not have to resize their window to argue with us.</summary>
        public static StageMode LayoutMode = StageMode.Auto;

        /// <summary>How much larger than authored the small type is drawn, 0 to 1. Nothing to
        /// do with UiScale: that one takes the whole stage down and keeps every proportion,
        /// this one changes a proportion on purpose, because the smallest labels were set for
        /// a page rather than for a screen two feet away. See Ursine.Text.TextScale.</summary>
        public static float TextSize;

        static bool _loaded;
        static bool _pending;
        static float _dueAt;
        const float Quiet = 0.5f;

        // ---- reading and applying ---------------------------------------------------

        /// <summary>Reads the saved settings (once) and puts every one of them into effect.
        /// Anything not saved keeps its default.</summary>
        public static void Load()
        {
            if (!_loaded)
            {
                _loaded = true;
                string text = SaveStore.Read(Slot);
                if (!string.IsNullOrEmpty(text))
                {
                    try
                    {
                        if (MiniJson.Parse(text) is JsonObject o)
                        {
                            Master = Level(o["master"], Master);
                            Music = Level(o["music"], Music);
                            Effects = Level(o["effects"], Effects);
                            Muted = o["muted"] is bool m ? m : Muted;
                            if (o["palette"] is string p)
                            {
                                int i = Array.IndexOf(PaletteNames, p);
                                if (i >= 0) PaletteIndex = i;
                            }
                            if (o["contrast"] is double c) ContrastLevel = (Contrast)Mathf.Clamp((int)c, 0, 2);
                            if (o["uiScale"] is double u) UiScale = Mathf.Clamp((float)u, FixedStage.MinScale, 1f);
                            // 2 was simply "Compact" before there were two of them, and still
                            // reads as Compact 1, which is what it was.
                            if (o["layout"] is double g) LayoutMode = (StageMode)Mathf.Clamp((int)g, 0, 3);
                            if (o["textSize"] is double x) TextSize = Mathf.Clamp01((float)x);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[What Lies In The Depths] The settings could not be read (" + e.Message + "); using the defaults.");
                    }
                }
            }
            ApplyAll();
        }

        static float Level(object v, float fallback) => v is double d ? Mathf.Clamp01((float)d) : fallback;

        public static void ApplyAll()
        {
            ApplyPalette();
            Theme.Contrast = ContrastLevel;
            ApplySound();
            ApplyStage();
        }

        public static Palette PaletteAt(int index)
        {
            if (index < 0 || index >= PaletteNames.Length) return null;
            return Resources.Load<Palette>($"{Theme.PaletteResourceFolder}/Palette_{PaletteNames[index]}");
        }

        static void ApplyPalette()
        {
            var p = PaletteAt(PaletteIndex);
            if (p != null) Theme.Use(p);
        }

        /// <summary>The stage is found through its static rather than hunted for, the same
        /// way the music is. Settings are loaded from GameState's Start, by which time every
        /// Awake has run, so the stage is there — but a scene without one is not an error.</summary>
        public static void ApplyStage()
        {
            TextScale.Set(TextSize);
            if (FixedStage.I != null) FixedStage.I.SetUserScale(UiScale);
            // The director may not be awake on the very first call; it reads the mode from
            // here in its own Start, so between the two of them it is always applied once.
            if (UI.StageDirector.I != null) UI.StageDirector.I.SetMode(LayoutMode);
        }

        /// <summary>Master multiplies, it does not override: a master at half and music at
        /// half is a quarter. Effects has nothing to carry yet, so nothing listens to it.</summary>
        public static void ApplySound()
        {
            if (Jukebox.I != null) Jukebox.I.SetVolume(Master * Music, Muted);
        }

        // ---- changing ---------------------------------------------------------------

        public static void SetVolumes(float master, float music, float effects)
        {
            Master = Mathf.Clamp01(master);
            Music = Mathf.Clamp01(music);
            Effects = Mathf.Clamp01(effects);
            ApplySound();
            RequestSave();
        }

        public static void SetMuted(bool muted)
        {
            Muted = muted;
            ApplySound();
            RequestSave();
        }

        public static void SetPalette(int index)
        {
            if (index < 0 || index >= PaletteNames.Length) return;
            PaletteIndex = index;
            ApplyPalette();
            RequestSave();
        }

        public static void SetUiScale(float scale)
        {
            UiScale = Mathf.Clamp(scale, FixedStage.MinScale, 1f);
            ApplyStage();
            RequestSave();
        }

        public static void SetTextSize(float amount)
        {
            TextSize = Mathf.Clamp01(amount);
            TextScale.Set(TextSize);
            RequestSave();
        }

        public static void SetLayoutMode(StageMode mode)
        {
            LayoutMode = mode;
            if (UI.StageDirector.I != null) UI.StageDirector.I.SetMode(mode);
            RequestSave();
        }

        public static void SetContrast(Contrast level)
        {
            ContrastLevel = level;
            Theme.Contrast = level;
            RequestSave();
        }

        // ---- saving -----------------------------------------------------------------

        /// <summary>Asks for a save half a second from now; another change in that time
        /// moves it back.</summary>
        public static void RequestSave()
        {
            _pending = true;
            _dueAt = Time.unscaledTime + Quiet;
        }

        /// <summary>Called every frame by the game: writes a requested save once things have
        /// been still for half a second.</summary>
        public static void Flush()
        {
            if (_pending && Time.unscaledTime >= _dueAt) Save();
        }

        public static void Save()
        {
            _pending = false;
            var o = new JsonObject();
            o["master"] = (double)Master;
            o["music"] = (double)Music;
            o["effects"] = (double)Effects;
            o["muted"] = Muted;
            o["palette"] = PaletteNames[Mathf.Clamp(PaletteIndex, 0, PaletteNames.Length - 1)];
            o["contrast"] = (double)(int)ContrastLevel;
            o["uiScale"] = (double)UiScale;
            o["layout"] = (double)(int)LayoutMode;
            o["textSize"] = (double)TextSize;
            SaveStore.Write(Slot, MiniJson.Write(o));
        }
    }
}
