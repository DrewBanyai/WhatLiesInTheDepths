// What Lies In The Depths — the dream debugger.
//
// Tools > What Lies In The Depths > Dream Debugger (Ctrl+Shift+D). Play mode only: it holds no
// state of its own and never writes to an asset, it only reaches into the running dream and
// calls Dirty(), which is the same door the game's own buttons go through. Every view then
// redraws from state, so a fathom added here writes the next paragraph and a fathom taken back
// un-writes it, exactly as the game would.
//
// Nothing here is a shortcut past the rules: mustering still spends, realizing still spends.
// Where a control breaks a rule on purpose — a fathom for free, an unlock taken back — it is
// because a debugger's whole job is to reach a state the game would take an hour to reach.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Ursine.Audio;
using Ursine.Economy;
using WhatLiesInTheDepths.Data;

namespace WhatLiesInTheDepths.EditorTools
{
    public sealed class DreamDebugWindow : EditorWindow
    {
        const string Root = "Tools/What Lies In The Depths/";

        Vector2 _scroll;
        string _unlockFilter = string.Empty;
        readonly Dictionary<string, bool> _open = new Dictionary<string, bool>();

        /// <summary>The widest a row is allowed to be. A debugger is read down a column; let it
        /// stretch to a maximized window and the label and its button end up a screen apart.</summary>
        const float Column = 520f;

        [MenuItem(Root + "Dream Debugger %#d", priority = 100)]
        public static void Open()
        {
            bool fresh = !HasOpenInstances<DreamDebugWindow>();
            var w = GetWindow<DreamDebugWindow>(false, "Dream Debugger", true);
            w.minSize = new Vector2(360f, 320f);
            if (fresh) w.position = new Rect(w.position.x, w.position.y, Column + 20f, 700f);
            w.Show();
        }

        void OnEnable() => EditorApplication.playModeStateChanged += _ => Repaint();

        void OnInspectorUpdate() => Repaint();      // the dream moves on its own; so does this

        static Dream Dream => GameState.I != null ? GameState.I.Dream : null;

        void OnGUI()
        {
            if (!Application.isPlaying || Dream == null)
            {
                EditorGUILayout.HelpBox("The dream runs in play mode. Press Play, and this fills in.",
                                        MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(Column)))
                {
                    Speed();
                    Veil();
                    Revelations();
                    Visions();
                    Assault();
                    Constructs();
                    FocusCards();
                    Purse();
                    Music();
                    UnlockList();
                    EditorGUILayout.Space(8f);
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndScrollView();
        }

        // ---- sections ---------------------------------------------------------------

        void Veil()
        {
            if (!Section("veil", "The veil")) return;
            var d = Dream;
            var v = d.veil;
            if (v == null) { EditorGUILayout.LabelField("No veil in this dream."); return; }

            EditorGUILayout.LabelField($"{v.n}  ·  veil {v.ord}  ·  {v.sunk:0} of {v.need:0} fathoms");
            EditorGUILayout.LabelField($"written: {v.revealed} of {(v.entry?.Count ?? 0)} paragraphs"
                                     + (v.AtFull ? "  ·  sounded" : string.Empty));

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(v.sunk <= 0))
                    if (GUILayout.Button("− fathom")) Sink(-1);
                using (new EditorGUI.DisabledScope(v.AtFull))
                    if (GUILayout.Button("+ fathom")) Sink(1);
                using (new EditorGUI.DisabledScope(v.sunk <= 0))
                    if (GUILayout.Button("−10")) Sink(-10);
                using (new EditorGUI.DisabledScope(v.AtFull))
                    if (GUILayout.Button("+10")) Sink(10);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Empty")) SetSunk(0);
                if (GUILayout.Button("Sound it")) SetSunk(v.need);
                using (new EditorGUI.DisabledScope(!(v.AtFull && d.HasNextVeil)))
                    if (GUILayout.Button("Part the veil")) { d.PartVeil(); Changed(); }
            }

            double want = EditorGUILayout.DoubleField("Fathoms sunk", v.sunk);
            if (!Mathf.Approximately((float)want, (float)v.sunk)) SetSunk(want);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Oneiri bound {v.w} of {v.cap}", GUILayout.Width(150f));
                using (new EditorGUI.DisabledScope(v.w <= 0))
                    if (GUILayout.Button("−")) { v.w -= 1; Changed(); }
                using (new EditorGUI.DisabledScope(v.w >= v.cap))
                    if (GUILayout.Button("+")) { v.w += 1; Changed(); }
                bool attend = GUILayout.Toggle(d.attendingDive, "your attention", "Button");
                if (attend != d.attendingDive)
                {
                    d.attendingDive = attend;
                    if (attend) d.attendedTaskId = null;
                    Changed();
                }
            }

            float p = EditorGUILayout.Slider("Toward the next dive", v.p, 0f, 1f);
            if (!Mathf.Approximately(p, v.p)) { v.p = p; Changed(); }
        }

        /// <summary>A fathom, free. Everything a dive would bring or spend is left alone: this
        /// is a reader's shortcut to a depth, not a dive.</summary>
        static void Sink(double n) => SetSunk((Dream.veil?.sunk ?? 0) + n);

        static void SetSunk(double n)
        {
            var v = Dream.veil;
            if (v == null) return;
            double before = v.sunk;
            v.sunk = System.Math.Max(0, System.Math.Min(v.need, n));
            Dream.fathomsTotal += System.Math.Max(0, v.sunk - before);
            if (v.sunk < before) v.p = 0f;
            Changed();
        }

        void Revelations()
        {
            var d = Dream;
            if (!Section("revelations", $"Revelations ({d.revelations.Count(r => r.realized)} realized of {d.revelations.Count})")) return;

            foreach (var r in d.revelations)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((r.great ? "◆ " : "") + r.n
                                             + (d.Shown(r) ? string.Empty : "  (not shown)"),
                                               GUILayout.MinWidth(120f));
                    if (r.realized)
                    {
                        if (GUILayout.Button("Un-realize", GUILayout.Width(88f)))
                        {
                            r.realized = false;
                            d.absorbed.Remove(r.k);
                            if (r.withdraws != null)
                                foreach (var w in r.withdraws) d.unlocks.EditorRemove("withdrawn:" + w);
                            Forget("rev:" + r.k, r.grants);
                            Changed();
                        }
                    }
                    else if (GUILayout.Button("Realize", GUILayout.Width(88f)))
                    {
                        // Free: the debugger reaches the state, it does not pay for it.
                        r.realized = true;
                        if (r.great && !d.absorbed.Contains(r.k)) d.absorbed.Add(r.k);
                        d.unlocks.Add("rev:" + r.k);
                        d.Withdraw(r);
                        d.unlocks.AddRange(r.grants);
                        Changed();
                    }
                    Shows("shown:rev:" + r.k);
                }
            }
        }

        void Visions()
        {
            var d = Dream;
            if (!Section("visions", $"Visions ({d.visions.Count} on the eye, {d.visionsAbsorbed.Count} kept)")) return;

            foreach (var v in d.visions.ToList())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField((v.great ? "◆ " : "") + v.n
                                             + (d.Shown(v) ? string.Empty : "  (not shown)")
                                             + $"  {v.p:0}%", GUILayout.MinWidth(120f));
                    float p = EditorGUILayout.Slider(v.p, 0f, 100f, GUILayout.Width(110f));
                    if (!Mathf.Approximately(p, v.p)) { v.p = p; Changed(); }
                    bool channel = GUILayout.Toggle(v.a, "pour", "Button", GUILayout.Width(46f));
                    if (channel != v.a) { v.a = channel; Changed(); }
                    if (GUILayout.Button("Finish", GUILayout.Width(56f))) { v.p = 100f; Changed(); }
                    if (GUILayout.Button("Reset", GUILayout.Width(52f)))
                    {
                        v.p = 0f; v.done = 0; v.a = false; Changed();
                    }
                    Shows("shown:vision:" + v.k);
                }
            }

            if (d.visionsAbsorbed.Count > 0)
            {
                EditorGUILayout.LabelField("Kept in the iris:", EditorStyles.miniBoldLabel);
                foreach (var v in d.visionsAbsorbed.ToList())
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("  " + v.n, GUILayout.MinWidth(120f));
                        if (GUILayout.Button("Put it back", GUILayout.Width(88f)))
                        {
                            d.visionsAbsorbed.Remove(v);
                            if (!d.visions.Contains(v)) d.visions.Add(v);
                            v.p = 0f;
                            Changed();
                        }
                    }
            }
        }

        void Assault()
        {
            var d = Dream;
            if (!Section("assault", $"Assault ({d.road.Count(l => l.won)} of {d.road.Count} taken)")) return;

            foreach (var l in d.road)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{l.n}  ({l.en:0} strong)", GUILayout.MinWidth(150f));
                    if (l.won)
                    {
                        if (GUILayout.Button("Give it back", GUILayout.Width(96f)))
                        {
                            l.won = false;
                            l.tookWith = 0;
                            if (l.costScale > 0 && l.costScale != 1.0) d.musterCostScale /= l.costScale;
                            Changed();
                        }
                    }
                    else if (GUILayout.Button("Take it", GUILayout.Width(96f)))
                    {
                        // Exactly what winning it does: the discount, the unit, the grants.
                        d.TakePlace(l, Army());
                        Changed();
                    }
                }
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField($"Your army: {Army():0}", EditorStyles.miniBoldLabel);
            foreach (var u in d.units)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{u.n}  ×{u.c}  ({u.p:0} each)", GUILayout.MinWidth(150f));
                    using (new EditorGUI.DisabledScope(u.c <= 0))
                        if (GUILayout.Button("−", GUILayout.Width(26f))) { u.c -= 1; Changed(); }
                    if (GUILayout.Button("+", GUILayout.Width(26f))) { u.c += 1; Changed(); }
                    if (GUILayout.Button("+10", GUILayout.Width(40f))) { u.c += 10; Changed(); }
                    Shows("shown:unit:" + u.k);
                }
            }
        }

        static double Army() => Dream.Army;

        void Constructs()
        {
            var d = Dream;
            if (!Section("constructs", $"Constructs ({d.constructs.Count(c => c.owned > 0)} standing)")) return;

            foreach (var c in d.constructs)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{c.n}  ×{c.owned}", GUILayout.MinWidth(150f));
                    using (new EditorGUI.DisabledScope(c.owned <= 0))
                        if (GUILayout.Button("−", GUILayout.Width(26f)))
                        {
                            c.owned -= 1;
                            c.built = c.owned > 0;
                            if (c.owned == 0) Forget("built:" + c.g, c.grants);
                            Changed();
                        }
                    if (GUILayout.Button("+", GUILayout.Width(26f)))
                    {
                        c.owned += 1;
                        c.built = true;
                        d.unlocks.Add("built:" + c.g);
                        d.unlocks.AddRange(c.grants);
                        Changed();
                    }
                    Shows("shown:construct:" + c.g);
                }
            }
        }

        static readonly float[] Speeds = { 1f, 5f, 10f, 25f };

        /// <summary>Focus work and the dive at 1x, 5x, 10x or 25x. Only Dream.Step is scaled:
        /// the second tick (passive rates, channelled Visions) keeps its own pace.</summary>
        void Speed()
        {
            var d = Dream;
            if (!Section("speed", $"Work speed ({d.workSpeed:0}x)")) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Focus and dives", GUILayout.Width(110f));
                foreach (var s in Speeds)
                {
                    bool on = Mathf.Approximately(d.workSpeed, s);
                    bool want = GUILayout.Toggle(on, s + "x", "Button", GUILayout.Width(44f));
                    if (want && !on) { d.workSpeed = s; Changed(); }
                }
            }
        }

        void FocusCards()
        {
            var d = Dream;
            if (!Section("focus", "Focus")) return;

            foreach (var t in d.tasks)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"{t.n}  ({t.w} of {t.cap} bound)", GUILayout.MinWidth(150f));
                    float p = EditorGUILayout.Slider(t.p, 0f, 100f, GUILayout.Width(110f));
                    if (!Mathf.Approximately(p, t.p)) { t.p = p; Changed(); }
                    bool mine = d.attendedTaskId == t.id;
                    bool want = GUILayout.Toggle(mine, "yours", "Button", GUILayout.Width(52f));
                    if (want != mine)
                    {
                        d.attendedTaskId = want ? t.id : null;
                        if (want) d.attendingDive = false;
                        Changed();
                    }
                    Shows("shown:focus:" + t.id);
                }
            }
        }

        void Purse()
        {
            var d = Dream;
            if (!Section("purse", "The purse")) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill everything")) { foreach (var r in d.resources) r.c = r.m; Changed(); }
                if (GUILayout.Button("Empty everything")) { foreach (var r in d.resources) r.c = 0; Changed(); }
            }

            foreach (var r in d.resources)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(r.n + (d.Shown(r) ? string.Empty : "  (not shown)"),
                                               GUILayout.MinWidth(110f));
                    double c = EditorGUILayout.DoubleField(r.c, GUILayout.Width(80f));
                    EditorGUILayout.LabelField("of", GUILayout.Width(16f));
                    double m = EditorGUILayout.DoubleField(r.m, GUILayout.Width(80f));
                    if (c != r.c || m != r.m)
                    {
                        r.m = System.Math.Max(0, m);
                        r.c = System.Math.Max(0, System.Math.Min(r.m <= 0 ? c : r.m, c));
                        Changed();
                    }
                    if (GUILayout.Button("max", GUILayout.Width(40f))) { r.c = r.m; Changed(); }
                    if (GUILayout.Button("0", GUILayout.Width(26f))) { r.c = 0; Changed(); }
                    Shows("shown:res:" + r.k);
                }
            }
        }

        // ---- the music ---------------------------------------------------------
        // Not part of the dream, but it is the other thing running, and a ten-minute track
        // is not something anyone can sit through to see the hand-over work. "To the
        // hand-over" drops the needle three seconds before the dissolve is due to start.

        void Music()
        {
            var box = Jukebox.I;
            if (!Section("music", "Music" + (box != null && box.Dissolving ? "  ·  dissolving" : string.Empty)))
                return;

            if (box == null) { EditorGUILayout.LabelField("No jukebox: this is a play-mode service."); return; }
            if (box.Playing == null) { EditorGUILayout.LabelField("Nothing playing — is Assets/Music empty?"); return; }

            EditorGUILayout.LabelField(box.PlayingName, EditorStyles.miniBoldLabel);

            var bar = EditorGUILayout.GetControlRect();
            float through = box.Length <= 0f ? 0f : box.Position / box.Length;
            EditorGUI.ProgressBar(bar, through, $"{Clock(box.Position)} of {Clock(box.Length)}");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Skip")) box.Skip();
                using (new EditorGUI.DisabledScope(box.Dissolving))
                    if (GUILayout.Button("To the hand-over"))
                        box.Seek(box.Length - box.crossfadeSeconds - 3f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                float v = EditorGUILayout.Slider("Level", box.volume, 0f, 1f);
                bool m = GUILayout.Toggle(box.muted, "mute", "Button", GUILayout.Width(52f));
                if (!Mathf.Approximately(v, box.volume) || m != box.muted) box.SetVolume(v, m);
            }

            if (box.Recent.Count <= 0) return;
            var names = new List<string>();
            foreach (var c in box.Recent) if (c != null) names.Add(c.name);
            EditorGUILayout.LabelField("Cannot come round next:", string.Join(", ", names));
        }

        static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return $"{s / 60}:{s % 60:00}";
        }

        void UnlockList()
        {
            var d = Dream;
            if (!Section("unlocks", $"Unlocks ({d.unlocks.Count})")) return;

            _unlockFilter = EditorGUILayout.TextField("Filter", _unlockFilter);
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var id in new[] { "col:ledger", "col:gauge", "bind:oneiri" })
                    Toggle(id, id.Substring(id.IndexOf(':') + 1));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var menu in new[] { "focus", "constructs", "revelations", "visions", "assault" })
                    Toggle("menu:" + menu, menu);
            }

            EditorGUILayout.Space(2f);
            foreach (var id in d.unlocks.All.OrderBy(x => x).ToList())
            {
                if (!string.IsNullOrEmpty(_unlockFilter)
                    && id.IndexOf(_unlockFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(id);
                    if (GUILayout.Button("forget", GUILayout.Width(56f))) { Forget(id, null); Changed(); }
                }
            }
        }

        // ---- plumbing ----------------------------------------------------------------

        /// <summary>A "shown" unlock as a small on/off, so a card can be made to appear or sent
        /// back where it came from.</summary>
        static void Shows(string id)
        {
            bool has = Dream.unlocks.Has(id);
            bool want = GUILayout.Toggle(has, "shown", "Button", GUILayout.Width(52f));
            if (want == has) return;
            if (want) Dream.unlocks.Add(id); else Dream.unlocks.EditorRemove(id);
            Changed();
        }

        static void Toggle(string id, string label)
        {
            bool has = Dream.unlocks.Has(id);
            bool want = GUILayout.Toggle(has, label, "Button");
            if (want == has) return;
            if (want) Dream.unlocks.Add(id); else Dream.unlocks.EditorRemove(id);
            Changed();
        }

        static void Forget(string id, IEnumerable<string> also)
        {
            Dream.unlocks.EditorRemove(id);
            if (also == null) return;
            foreach (var g in also) Dream.unlocks.EditorRemove(g);
        }

        /// <summary>The one door out of this window: the dream works out what now follows and
        /// every view redraws from it.</summary>
        static void Changed() => Dream.Dirty();

        /// <summary>A foldout remembered by a key rather than by its own words, because the
        /// words carry a count and the count moves.</summary>
        bool Section(string key, string title)
        {
            if (!_open.TryGetValue(key, out bool open)) open = key == "veil";
            bool now = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
            _open[key] = now;
            if (now) EditorGUILayout.Space(2f);
            return now;
        }
    }
}
