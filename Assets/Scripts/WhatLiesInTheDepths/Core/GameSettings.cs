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
            SaveStore.Write(Slot, MiniJson.Write(o));
        }
    }
}
